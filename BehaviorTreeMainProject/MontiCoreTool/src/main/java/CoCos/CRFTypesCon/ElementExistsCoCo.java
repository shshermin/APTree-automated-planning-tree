package CoCos.CRFTypesCon;

import crftypescon._ast.ASTPickUpHL;
import crftypescon._ast.ASTPlaceHL;
import crftypescon._cocos.CRFTypesConASTPickUpHLCoCo;
import crftypescon._cocos.CRFTypesConASTPlaceHLCoCo;
import de.se_rwth.commons.logging.Log;

public class ElementExistsCoCo implements CRFTypesConASTPickUpHLCoCo, CRFTypesConASTPlaceHLCoCo {

@Override
public void check(ASTPickUpHL node) {
    // Get the element name string (this is safe - just returns the String)
    String elementName = node.getObj();
    
    // Check if the symbol was resolved (this checks without throwing errors)
    if (!node.isPresentObjSymbol()) {
        Log.error("0xA001 Error: Element '" + elementName + "' is not defined in CRFConcreteInstances.bt! " +
                  "Available elements: beam1, beam2, lp1, plate1, r1, FP1", 
                  node.get_SourcePositionStart());
        return;
    }
    
    // Symbol exists, additional validation can go here
}
    @Override
    public void check(ASTPlaceHL node) {
        // Get the element name string (this is safe - just returns the String)
        String elementName = node.getObj();
        
        // Check if the symbol was resolved (this checks without throwing errors)
        if (!node.isPresentObjSymbol()) {
            Log.error("0xA002 Error: Element '" + elementName + "' is not defined in CRFConcreteInstances.bt! " +
                      "Available elements: beam1, beam2, lp1, plate1, r1, FP1", 
                      node.get_SourcePositionStart());
            return;
        }
        
        // Symbol exists, additional validation can go here
    }
}
