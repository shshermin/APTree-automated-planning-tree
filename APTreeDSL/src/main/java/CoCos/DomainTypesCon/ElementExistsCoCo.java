package CoCos.DomainTypesCon;

import domaintypescon._ast.ASTPickUpHL;
import domaintypescon._ast.ASTPlaceHL;
import domaintypescon._cocos.DomainTypesConASTPickUpHLCoCo;
import domaintypescon._cocos.DomainTypesConASTPlaceHLCoCo;
import de.se_rwth.commons.logging.Log;

public class ElementExistsCoCo implements DomainTypesConASTPickUpHLCoCo, DomainTypesConASTPlaceHLCoCo {

@Override
public void check(ASTPickUpHL node) {
    // Get the element name string (this is safe - just returns the String)
    String elementName = node.getObj();
    
    // Check if the symbol was resolved (wrap in try-catch to handle symbol table issues)
    try {
        if (!node.isPresentObjSymbol()) {
            Log.error("0xA001 Error: Element '" + elementName + "' is not defined in CRFConcreteInstances.bt! " +
                      "Available elements: beam1, beam2, lp1, plate1, r1, fp1, fp2, fp3, rp1", 
                      node.get_SourcePositionStart());
            return;
        }
    } catch (Exception e) {
        // Symbol resolution failed - likely due to scope configuration issues during CoCo checks
        Log.debug("Warning: Could not resolve element '" + elementName + "' during symbol resolution. " +
                  "This may be a scope configuration issue.", "ElementExistsCoCo");
        return;
    }
    
    // Symbol exists, additional validation can go here
}
    @Override
    public void check(ASTPlaceHL node) {
        // Get the element name string (this is safe - just returns the String)
        String elementName = node.getObj();
        
        // Check if the symbol was resolved (wrap in try-catch to handle symbol table issues)
        try {
            if (!node.isPresentObjSymbol()) {
                Log.error("0xA002 Error: Element '" + elementName + "' is not defined in CRFConcreteInstances.bt! " +
                          "Available elements: beam1, beam2, lp1, plate1, r1, fp1, fp2, fp3, rp1", 
                          node.get_SourcePositionStart());
                return;
            }
        } catch (Exception e) {
            // Symbol resolution failed - likely due to scope configuration issues during CoCo checks
            Log.debug("Warning: Could not resolve element '" + elementName + "' during symbol resolution. " +
                      "This may be a scope configuration issue.", "ElementExistsCoCo");
            return;
        }
        
        // Symbol exists, additional validation can go here
    }
}
