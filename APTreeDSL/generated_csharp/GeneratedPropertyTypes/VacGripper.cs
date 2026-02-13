using System;
using System.Collections.Generic;

namespace BehaviorTree.Types {

    public class VacGripper : Tool {
        public Location loc { get; set; }
        public bool isActive { get; set; }
    }
}
