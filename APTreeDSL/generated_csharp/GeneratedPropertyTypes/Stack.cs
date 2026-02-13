using System;
using System.Collections.Generic;

namespace BehaviorTree.Types {

    public class Stack : Layer {
        public int level { get; set; }
        public Module belongsToModule { get; set; }
    }
}
