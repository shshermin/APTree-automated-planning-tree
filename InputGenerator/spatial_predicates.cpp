#include "spatial_predicates.h"
#include <CGAL/IO/io.h>
#include <fstream>
#include <sstream>
#include <algorithm>
#include <stdexcept>
#include <iostream>
#include <map>

// ---- SpatialObject implementation ----

SpatialObject::SpatialObject(const std::string& name, const Mesh& mesh)
    : name(name), mesh(mesh) {
    computeBbox();
}

void SpatialObject::computeBbox() {
    if (mesh.number_of_vertices() == 0) {
        bbox = CGAL::Bbox_3();
        return;
    }
    auto it = mesh.vertices_begin();
    bbox = mesh.point(*it).bbox();
    for (++it; it != mesh.vertices_end(); ++it) {
        bbox += mesh.point(*it).bbox();
    }
}

// ---- OBJ multi-object parser ----

std::vector<SpatialObject> loadMultiObjectOBJ(const std::string& filepath) {
    std::ifstream file(filepath);
    if (!file.is_open()) {
        throw std::runtime_error("Cannot open OBJ file: " + filepath);
    }

    // Global vertex list (OBJ indices are 1-based and global across all objects)
    std::vector<Point_3> allVertices;

    // Per-object data: name -> list of faces (each face = vector of global vertex indices)
    struct ObjData {
        std::string name;
        std::vector<std::vector<int>> faces;  // each face is a list of 1-based vertex indices
    };

    std::vector<ObjData> objects;
    ObjData* current = nullptr;

    std::string line;
    while (std::getline(file, line)) {
        if (line.empty() || line[0] == '#') continue;

        std::istringstream iss(line);
        std::string token;
        iss >> token;

        if (token == "o") {
            // New object
            objects.push_back(ObjData());
            current = &objects.back();
            iss >> current->name;
        }
        else if (token == "v") {
            double x, y, z;
            iss >> x >> y >> z;
            allVertices.emplace_back(x, y, z);
        }
        else if (token == "f") {
            if (!current) {
                // Faces before any 'o' line — create a default object
                objects.push_back(ObjData{"unnamed", {}});
                current = &objects.back();
            }
            std::vector<int> face;
            std::string vertexToken;
            while (iss >> vertexToken) {
                // Parse "v", "v/vt", "v/vt/vn", or "v//vn"
                int vIdx = std::stoi(vertexToken.substr(0, vertexToken.find('/')));
                face.push_back(vIdx);
            }
            current->faces.push_back(face);
        }
    }

    // Build SpatialObjects from parsed data
    std::vector<SpatialObject> result;
    for (auto& obj : objects) {
        Mesh mesh;

        // Map global vertex index -> local mesh vertex descriptor
        std::map<int, Mesh::Vertex_index> vertexMap;

        // Collect all vertex indices used by this object
        for (const auto& face : obj.faces) {
            for (int globalIdx : face) {
                if (vertexMap.find(globalIdx) == vertexMap.end()) {
                    const Point_3& p = allVertices[globalIdx - 1];  // OBJ is 1-based
                    vertexMap[globalIdx] = mesh.add_vertex(p);
                }
            }
        }

        // Add faces
        for (const auto& face : obj.faces) {
            if (face.size() == 3) {
                mesh.add_face(vertexMap[face[0]], vertexMap[face[1]], vertexMap[face[2]]);
            } else if (face.size() == 4) {
                // Triangulate the quad
                mesh.add_face(vertexMap[face[0]], vertexMap[face[1]], vertexMap[face[2]]);
                mesh.add_face(vertexMap[face[0]], vertexMap[face[2]], vertexMap[face[3]]);
            } else if (face.size() > 4) {
                // Fan triangulation for polygons
                for (size_t i = 1; i + 1 < face.size(); ++i) {
                    mesh.add_face(vertexMap[face[0]], vertexMap[face[i]], vertexMap[face[i + 1]]);
                }
            }
        }

        result.emplace_back(obj.name, mesh);
    }

    return result;
}

// ---- Loading and creation ----

SpatialObject loadMesh(const std::string& name, const std::string& filepath) {
    Mesh mesh;
    std::ifstream in(filepath);
    if (!in || !CGAL::IO::read_polygon_mesh(filepath, mesh)) {
        throw std::runtime_error("Failed to load mesh: " + filepath);
    }
    return SpatialObject(name, mesh);
}

SpatialObject makeBox(const std::string& name,
                      double xmin, double ymin, double zmin,
                      double xmax, double ymax, double zmax) {
    Mesh mesh;

    auto v0 = mesh.add_vertex(Point_3(xmin, ymin, zmin));
    auto v1 = mesh.add_vertex(Point_3(xmax, ymin, zmin));
    auto v2 = mesh.add_vertex(Point_3(xmax, ymax, zmin));
    auto v3 = mesh.add_vertex(Point_3(xmin, ymax, zmin));
    auto v4 = mesh.add_vertex(Point_3(xmin, ymin, zmax));
    auto v5 = mesh.add_vertex(Point_3(xmax, ymin, zmax));
    auto v6 = mesh.add_vertex(Point_3(xmax, ymax, zmax));
    auto v7 = mesh.add_vertex(Point_3(xmin, ymax, zmax));

    mesh.add_face(v0, v2, v1); mesh.add_face(v0, v3, v2);
    mesh.add_face(v4, v5, v6); mesh.add_face(v4, v6, v7);
    mesh.add_face(v0, v1, v5); mesh.add_face(v0, v5, v4);
    mesh.add_face(v3, v6, v2); mesh.add_face(v3, v7, v6);
    mesh.add_face(v0, v4, v7); mesh.add_face(v0, v7, v3);
    mesh.add_face(v1, v2, v6); mesh.add_face(v1, v6, v5);

    return SpatialObject(name, mesh);
}

// ---- Predicate helpers ----

// Get the min/max along the up axis
static double getUpMin(const CGAL::Bbox_3& b, UpAxis up) {
    switch (up) {
        case UpAxis::X: return b.xmin();
        case UpAxis::Y: return b.ymin();
        case UpAxis::Z: return b.zmin();
    }
    return b.ymin();
}

static double getUpMax(const CGAL::Bbox_3& b, UpAxis up) {
    switch (up) {
        case UpAxis::X: return b.xmax();
        case UpAxis::Y: return b.ymax();
        case UpAxis::Z: return b.zmax();
    }
    return b.ymax();
}

// Check if two bounding boxes overlap in the plane perpendicular to the up axis.
static bool footprintsOverlap(const CGAL::Bbox_3& a, const CGAL::Bbox_3& b, UpAxis up) {
    // Check overlap on the two axes that are NOT the up axis
    if (up == UpAxis::Y) {
        // Footprint is in XZ plane
        if (a.xmax() <= b.xmin() || b.xmax() <= a.xmin()) return false;
        if (a.zmax() <= b.zmin() || b.zmax() <= a.zmin()) return false;
    } else if (up == UpAxis::Z) {
        // Footprint is in XY plane
        if (a.xmax() <= b.xmin() || b.xmax() <= a.xmin()) return false;
        if (a.ymax() <= b.ymin() || b.ymax() <= a.ymin()) return false;
    } else { // UpAxis::X
        // Footprint is in YZ plane
        if (a.ymax() <= b.ymin() || b.ymax() <= a.ymin()) return false;
        if (a.zmax() <= b.zmin() || b.zmax() <= a.zmin()) return false;
    }
    return true;
}

// ---- Predicates ----

bool IsObjectClear(const SpatialObject& target,
                   const std::vector<SpatialObject>& scene,
                   UpAxis upAxis) {
    const double EPSILON = 1e-6;

    for (const auto& other : scene) {
        if (other.name == target.name) continue;

        bool isAbove = getUpMin(other.bbox, upAxis) >= getUpMax(target.bbox, upAxis) - EPSILON;
        bool overlaps = footprintsOverlap(target.bbox, other.bbox, upAxis);

        if (isAbove && overlaps) {
            return false;
        }
    }

    return true;
}

bool IsStackedOn(const SpatialObject& top,
                 const SpatialObject& bottom,
                 const std::vector<SpatialObject>& scene,
                 UpAxis upAxis) {
    if (top.name == bottom.name) return false;
    if (top.mesh.number_of_vertices() == 0 || bottom.mesh.number_of_vertices() == 0) return false;

    const double EPSILON = 1.0; // tolerance for touching faces

    double topMin = getUpMin(top.bbox, upAxis);
    double bottomMax = getUpMax(bottom.bbox, upAxis);

    // Top must sit at or just above bottom's upper face
    if (topMin < bottomMax - EPSILON || topMin > bottomMax + EPSILON) return false;

    // Footprints must overlap
    if (!footprintsOverlap(top.bbox, bottom.bbox, upAxis)) return false;

    // Check no other object sits between them
    for (const auto& other : scene) {
        if (other.name == top.name || other.name == bottom.name) continue;
        if (other.mesh.number_of_vertices() == 0) continue;

        double otherMin = getUpMin(other.bbox, upAxis);
        double otherMax = getUpMax(other.bbox, upAxis);

        // Other object is between bottom's top and top's bottom
        bool isBetween = (otherMin >= bottomMax - EPSILON) && (otherMax <= topMin + EPSILON);
        if (isBetween && footprintsOverlap(bottom.bbox, other.bbox, upAxis)
                      && footprintsOverlap(top.bbox, other.bbox, upAxis)) {
            return false;
        }
    }

    return true;
}

std::vector<std::pair<std::string, std::string>> FindAllStacked(
    const std::vector<SpatialObject>& scene,
    UpAxis upAxis) {
    std::vector<std::pair<std::string, std::string>> result;
    for (const auto& top : scene) {
        for (const auto& bottom : scene) {
            if (IsStackedOn(top, bottom, scene, upAxis)) {
                result.push_back({top.name, bottom.name});
            }
        }
    }
    return result;
}

bool ComputeStackContactCentroid(const SpatialObject& top,
                                 const SpatialObject& bottom,
                                 const std::vector<SpatialObject>& scene,
                                 StackContactInfo& info,
                                 UpAxis upAxis) {
    if (!IsStackedOn(top, bottom, scene, upAxis)) return false;

    const auto& a = top.bbox;
    const auto& b = bottom.bbox;

    // Contact height: midpoint between bottom's upper face and top's lower face
    double contactHeight = (getUpMax(b, upAxis) + getUpMin(a, upAxis)) / 2.0;

    double cx, cy, cz, area;

    if (upAxis == UpAxis::Y) {
        // Footprint is in XZ plane
        double xlo = std::max(a.xmin(), b.xmin());
        double xhi = std::min(a.xmax(), b.xmax());
        double zlo = std::max(a.zmin(), b.zmin());
        double zhi = std::min(a.zmax(), b.zmax());
        cx = (xlo + xhi) / 2.0;
        cy = contactHeight;
        cz = (zlo + zhi) / 2.0;
        area = (xhi - xlo) * (zhi - zlo);
    } else if (upAxis == UpAxis::Z) {
        // Footprint is in XY plane
        double xlo = std::max(a.xmin(), b.xmin());
        double xhi = std::min(a.xmax(), b.xmax());
        double ylo = std::max(a.ymin(), b.ymin());
        double yhi = std::min(a.ymax(), b.ymax());
        cx = (xlo + xhi) / 2.0;
        cy = (ylo + yhi) / 2.0;
        cz = contactHeight;
        area = (xhi - xlo) * (yhi - ylo);
    } else { // UpAxis::X
        // Footprint is in YZ plane
        double ylo = std::max(a.ymin(), b.ymin());
        double yhi = std::min(a.ymax(), b.ymax());
        double zlo = std::max(a.zmin(), b.zmin());
        double zhi = std::min(a.zmax(), b.zmax());
        cx = contactHeight;
        cy = (ylo + yhi) / 2.0;
        cz = (zlo + zhi) / 2.0;
        area = (yhi - ylo) * (zhi - zlo);
    }

    info.topName = top.name;
    info.bottomName = bottom.name;
    info.centroid = Point_3(cx, cy, cz);
    info.area = area;
    return true;
}

std::vector<StackContactInfo> FindAllStackedWithContact(
    const std::vector<SpatialObject>& scene,
    UpAxis upAxis) {
    std::vector<StackContactInfo> result;
    for (const auto& top : scene) {
        for (const auto& bottom : scene) {
            StackContactInfo info;
            if (ComputeStackContactCentroid(top, bottom, scene, info, upAxis)) {
                result.push_back(info);
            }
        }
    }
    return result;
}
