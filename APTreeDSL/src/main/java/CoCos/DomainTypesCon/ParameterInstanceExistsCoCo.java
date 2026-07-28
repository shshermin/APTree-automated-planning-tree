package CoCos.DomainTypesCon;

import java.lang.reflect.Method;

import domaintypesdef._ast.ASTPActionNode;
import domaintypesdef._cocos.DomainTypesDefASTPActionNodeCoCo;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo: All action parameter symbol references must resolve to known instances.
 *
 * Uses MontiCore's generated isPresentXxxSymbol() methods to check that every
 * parameter annotated with @Type in the grammar (e.g., obj:Name@Element) resolves
 * to a symbol loaded into the global scope.
 *
 * Catches:
 *   - EID6:  Unknown instance (e.g., lp24 doesn't exist)
 *   - EID8:  Wrong type (e.g., rp is not a FirstPos)
 *   - EID10: Wrong type (e.g., human is not a Robot)
 *
 * Error code: 0xDF020
 */
public class ParameterInstanceExistsCoCo implements DomainTypesDefASTPActionNodeCoCo {

    @Override
    public void check(ASTPActionNode node) {
        String actionType = node.getClass().getSimpleName();
        if (actionType.startsWith("AST")) {
            actionType = actionType.substring(3);
        }
        String actionName;
        try {
            actionName = node.getName();
        } catch (Exception e) {
            actionName = "<unnamed>";
        }
        if (actionName == null) actionName = "<unnamed>";

        System.out.println("[DEBUG] ParameterInstanceExistsCoCo checking: " + actionType + " " + actionName);

        // List all isPresent methods for debugging
        for (Method m : node.getClass().getMethods()) {
            if (m.getName().startsWith("isPresent") && m.getName().endsWith("Symbol") && m.getParameterCount() == 0) {
                System.out.println("[DEBUG]   Available method: " + m.getName());
            }
        }

        // Find all isPresentXxxSymbol() methods via reflection
        for (Method isPresent : node.getClass().getMethods()) {
            String methodName = isPresent.getName();

            // Match pattern: isPresent{Param}Symbol
            if (!methodName.startsWith("isPresent") || !methodName.endsWith("Symbol")) continue;
            if (methodName.equals("isPresentSymbol")) continue; // skip the node's own symbol
            if (methodName.equals("isPresentSubtreeAnnotationSymbol")) continue; // optional, not a parameter
            if (isPresent.getParameterCount() != 0) continue;

            // Extract parameter name: isPresentObjSymbol → Obj
            String paramName = methodName.substring("isPresent".length(),
                    methodName.length() - "Symbol".length());
            String getterName = "get" + paramName;

            try {
                boolean resolved = (boolean) isPresent.invoke(node);
                System.out.println("[DEBUG]   " + methodName + " = " + resolved);

                if (!resolved) {
                    String paramValue = "<unknown>";
                    try {
                        Method isPresentAttr = null;
                        try {
                            isPresentAttr = node.getClass().getMethod("isPresent" + paramName);
                        } catch (NoSuchMethodException ignored) {}

                        if (isPresentAttr == null || (boolean) isPresentAttr.invoke(node)) {
                            Method getter = node.getClass().getMethod(getterName);
                            Object value = getter.invoke(node);
                            if (value != null) paramValue = value.toString();
                        }
                    } catch (Exception ignored) {}

                    String displayParam = Character.toLowerCase(paramName.charAt(0)) + paramName.substring(1);

                    Log.error("0xDF020 Parameter '" + displayParam + "' in action " + actionType +
                            " '" + actionName + "': instance '" + paramValue +
                            "' is not defined or has the wrong type. " +
                            "Check that it exists in the setup model with the correct type.",
                            node.get_SourcePositionStart());
                }
            } catch (Exception e) {
                System.out.println("[DEBUG]   EXCEPTION on " + methodName + ": " + e.getClass().getSimpleName() + " - " + e.getMessage());
            }
        }
    }
}
