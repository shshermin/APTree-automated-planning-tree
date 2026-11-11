# Third-party software

APTree itself is licensed under the Apache License 2.0 (see [LICENSE](LICENSE)).
The following third-party software is shipped in this repository under its own license.

## ENHSP

- File: `APTreeExecutionEngine/python_service/enhsp.jar` (also copied into the Docker image)
- Project: ENHSP, Expressive Numeric Heuristic Search Planner, by Enrico Scala
- License: GNU General Public License v3.0 or later, full text in
  [`APTreeExecutionEngine/python_service/ENHSP_LICENSE.txt`](APTreeExecutionEngine/python_service/ENHSP_LICENSE.txt)
- Source code: https://gitlab.com/enricos83/ENHSP-Public

The planner service runs ENHSP as a separate process (`java -jar enhsp.jar`); APTree does
not link against it. The jar bundles the license files of its own dependencies under
`META-INF/`.
