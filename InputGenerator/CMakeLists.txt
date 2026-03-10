cmake_minimum_required(VERSION 3.20)
project(InputGenerator LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(CGAL CONFIG REQUIRED)

add_executable(InputGenerator main.cpp spatial_predicates.cpp PDDLPredicateGenerator.cpp PredicateGenerator.cpp)
target_link_libraries(InputGenerator PRIVATE CGAL::CGAL)
