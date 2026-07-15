package CoCos.DomainTypesDef;

import java.util.Set;

import domaintypesdef._ast.ASTPropertyTypeDefinition;
import domaintypesdef._cocos.DomainTypesDefASTPropertyTypeDefinitionCoCo;
import de.se_rwth.commons.logging.Log;

public class NewTypesInheritFromCustomTypes implements DomainTypesDefASTPropertyTypeDefinitionCoCo{

 
 // Define the whitelist of allowed supertypes
  private static final Set<String> ALLOWED_TYPES = Set.of(
      "Agent", 
      "Element", 
      "Tool", 
      "Location"
  );

  @Override
  public void check(ASTPropertyTypeDefinition node) {
    String superType = node.getSuperType();

    // Check if the superType provided in the model is in our allowed set
    if (!ALLOWED_TYPES.contains(superType)) {
      Log.error(
          String.format("0xCF001: '%s' is not a valid supertype. " +
                        "Property types must inherit from one of: %s", 
                        superType, ALLOWED_TYPES),
          node.get_SourcePositionStart()
      );
    }
  }
}
 

    

