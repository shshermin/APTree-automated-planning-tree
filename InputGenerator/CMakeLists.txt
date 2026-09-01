cmake_minimum_required(VERSION 3.20)
project(InputGenerator LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(CGAL CONFIG REQUIRED)

add_executable(InputGenerator main.cpp spatial_predicates.cpp PDDLPredicateGenerator.cpp PredicateGenerator.cpp)
target_link_libraries(InputGenerator PRIVATE CGAL::CGAL)

# ---- Tests ----
# spatial_predicates.{h,cpp} pulls in CGAL directly, so the test binary needs
# the same CGAL dependency as the main executable (no way to isolate the pure
# math from CGAL types without a header refactor — out of scope for Phase 0).
option(BUILD_TESTS "Build the InputGenerator test suite" ON)

if(BUILD_TESTS)
  include(FetchContent)
  FetchContent_Declare(
    googletest
    URL https://github.com/google/googletest/archive/refs/tags/v1.15.2.zip
  )
  # Keep gtest's own runtime settings consistent with the rest of the build.
  set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
  FetchContent_MakeAvailable(googletest)

  enable_testing()

  add_executable(InputGeneratorTests
    tests/spatial_predicates_test.cpp
    spatial_predicates.cpp
  )
  target_link_libraries(InputGeneratorTests PRIVATE CGAL::CGAL GTest::gtest_main)
  target_include_directories(InputGeneratorTests PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

  include(GoogleTest)
  gtest_discover_tests(InputGeneratorTests)
endif()
