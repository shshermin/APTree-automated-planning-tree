package CoCos.DomainTypesCon;

import java.lang.reflect.Method;

import domaintypesdef._ast.ASTPActionNode;
import domaintypesdef._cocos.DomainTypesDefASTPActionNodeCoCo;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo: All action parameter symbol references must resolve to known instances.
 *
 * Uses MontiCore's generated isPresentXxxSymbol() methods via reflection to
 * generically check that every parameter annotated with @Type in the grammar
 * resolves to a symbol loaded into the global scope.
 *
 * This approach is fully generic: adding a new action type to the grammar
 * requires NO changes to this CoCo.
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

        // Discover all isPresentXxxSymbol() methods via reflection
        for (Method isPresent : node.getClass().getMethods()) {
            String methodName = isPresent.getName();
            if (!methodName.startsWith("isPresent") || !methodName.endsWith("Symbol")) continue;
            if (methodName.equals("isPresentSymbol")) continue;
            if (methodName.equals("isPresentSubtreeAnnotationSymbol")) continue;
            if (isPresent.getParameterCount() != 0) continue;

            // Extract parameter name: isPresentObjSymbol → obj
            String paramPascal = methodName.substring("isPresent".length(),
                    methodName.length() - "Symbol".length());
            String paramName = Character.toLowerCase(paramPascal.charAt(0)) + paramPascal.substring(1);

            boolean resolved = safeIsPresent(isPresent, node);
            if (!resolved) {
                String paramValue = safeGetParamValue(node, paramPascal);
                String expectedType = getExpectedType(node, paramPascal);
                Log.error("0xDF020 Parameter '" + paramName + "' in action " + actionType +
                        " '" + actionName + "': instance '" + paramValue +
                        "' does not resolve to a known " + expectedType + ". " +
                        "Check that it exists in the setup model with the correct type.",
                        node.get_SourcePositionStart());
            }
        }
    }

    /**
     * Safely invoke isPresentXxxSymbol() via reflection.
     * Catches MontiCore internal errors and suppresses 0xA7003/0xA7303 findings.
     */
    private boolean safeIsPresent(Method isPresentMethod, ASTPActionNode node) {
        try {
            boolean result = (boolean) isPresentMethod.invoke(node);
            return result;
        } catch (Exception e) {
            return false;
        } finally {
            Log.getFindings().removeIf(f -> f.isError()
                && (f.getMsg().contains("0xA7003") || f.getMsg().contains("0xA7303")));
        }
    }

    /**
     * Get the raw parameter value (the name string as written in the model).
     * Calls getXxx() on the node (e.g., getObj(), getClient()).
     */
    private String safeGetParamValue(ASTPActionNode node, String paramPascal) {
        try {
            Method getter = node.getClass().getMethod("get" + paramPascal);
            Object value = getter.invoke(node);
            return value != null ? value.toString() : "<unknown>";
        } catch (Exception e) {
            return "<unknown>";
        }
    }

    /**
     * Derive the expected type name from the getXxxSymbol() return type.
     * e.g., getObjSymbol() returns ElementSymbol → "Element"
     */
    private String getExpectedType(ASTPActionNode node, String paramPascal) {
        try {
            Method getter = node.getClass().getMethod("get" + paramPascal + "Symbol");
            String typeName = getter.getReturnType().getSimpleName();
            if (typeName.endsWith("Symbol")) {
                typeName = typeName.substring(0, typeName.length() - "Symbol".length());
            }
            return typeName;
        } catch (Exception e) {
            return "symbol";
        }
    }
}
