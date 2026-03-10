#ifndef SPATIAL_PREDICATES_H
#define SPATIAL_PREDICATES_H

#include <string>
#include <vector>
#include <CGAL/Exact_predicates_inexact_constructions_kernel.h>
#include <CGAL/Surface_mesh.h>
#include <CGAL/Bbox_3.h>

typedef CGAL::Exact_predicates_inexact_constructions_kernel K;
typedef K::Point_3 Point_3;
typedef CGAL::Surface_mesh<Point_3> Mesh;

// Which axis points "up" (used for on-top checks)
enum class UpAxis { X, Y, Z };

// Represents a named 3D mesh object in the scene.
struct SpatialObject {
    std::string name;
    Mesh mesh;

    // Cached bounding box (call computeBbox() after loading/modifying the mesh)
    CGAL::Bbox_3 bbox;

    SpatialObject() = default;
    SpatialObject(const std::string& name, const Mesh& mesh);

    // Recompute the bounding box from the mesh vertices
    void computeBbox();

    double z_min() const { return bbox.zmin(); }
    double z_max() const { return bbox.zmax(); }
};

// Load all named objects from a multi-object OBJ file.
// Objects are separated by "o <name>" lines.
std::vector<SpatialObject> loadMultiObjectOBJ(const std::string& filepath);

// Load a single mesh from file (supports OFF, OBJ, STL, PLY)
SpatialObject loadMesh(const std::string& name, const std::string& filepath);

// Create a box mesh programmatically (useful for testing)
SpatialObject makeBox(const std::string& name,
                      double xmin, double ymin, double zmin,
                      double xmax, double ymax, double zmax);

// Returns true if no other object in the scene is on top of the target object.
// "On top" means the other object's footprint (perpendicular to upAxis) overlaps
// the target's, AND the other object sits at or above the target's top along upAxis.
bool IsObjectClear(const SpatialObject& target,
                   const std::vector<SpatialObject>& scene,
                   UpAxis upAxis = UpAxis::Y);

// Returns true if 'top' is directly stacked on 'bottom'.
// "Directly stacked" means:
//   1) top sits immediately above bottom (top's lower face touches bottom's upper face)
//   2) Their footprints overlap in the plane perpendicular to upAxis
//   3) No other object sits between them along the up axis
bool IsStackedOn(const SpatialObject& top,
                 const SpatialObject& bottom,
                 const std::vector<SpatialObject>& scene,
                 UpAxis upAxis = UpAxis::Y);

// Returns all (top, bottom) pairs where top is directly stacked on bottom.
std::vector<std::pair<std::string, std::string>> FindAllStacked(
    const std::vector<SpatialObject>& scene,
    UpAxis upAxis = UpAxis::Y);

#endif // SPATIAL_PREDICATES_H
