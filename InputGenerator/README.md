# InputGenerator

C++ tool that reads a scene from a Wavefront OBJ file, derives spatial
predicates from the mesh geometry (which objects are clear, which are stacked on
each other) and writes them as a PDDL initial state and as APTree DSL
init/goal states.

## Requirements

- CMake 3.20 or newer and a C++17 compiler
- [CGAL](https://www.cgal.org/) (found via `find_package(CGAL CONFIG)`), e.g.
  `sudo apt install libcgal-dev` on Ubuntu, or vcpkg on Windows
- GoogleTest is downloaded by CMake for the test build

## Build

From this directory:

```bash
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build
```

Pass `-DBUILD_TESTS=OFF` to skip the test suite.

## Run

Run from this directory, the default paths are relative to it:

```bash
./build/InputGenerator [scene.obj] [properties.bt] [init-state.bt] [goal-state.bt]
```

| Argument | Default |
|---|---|
| `scene.obj` | `test meshes.obj` |
| `properties.bt` | `../APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorProperties.bt` |
| `init-state.bt` | `../APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorInitState.bt` |
| `goal-state.bt` | `../APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorGoalState.bt` |

Outputs:

- `InitialState.pddl` and `StackContactCentroids.csv` in the current directory
- the DSL init and goal states at the paths above. With the defaults these
  overwrite the demonstrator fixtures in `APTreeDSL`, so pass your own paths
  unless that is what you want.

The geometry is evaluated with Y as the up axis (OBJ convention).

## Tests

```bash
cd build && ctest --output-on-failure
```

`StackDetector.cs` is a Grasshopper script with Rhino dependencies and is not
part of the automated suite; see
[tests/MANUAL_GRASSHOPPER_TESTS.md](tests/MANUAL_GRASSHOPPER_TESTS.md) for its
manual checklist.
