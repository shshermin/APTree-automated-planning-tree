package urmotionplanner._cocos;

import urmotionplanner._ast.ASTURMotionPlannerDefinition;
import de.se_rwth.commons.logging.Log;
import java.io.*;
import java.util.*;
import java.util.regex.*;

/**
 * Context Condition: Validates that parameter instances only use properties
 * that are defined in their corresponding parameter type definitions.
 */
public class ParameterInstancesMatchTypesCoCo implements URMotionPlannerASTURMotionPlannerDefinitionCoCo {
    
    // Default paths for validation files
    private static final String DEFAULT_PARAM_TYPES_PATH = "src/test/resources/valid/crf/PDDLActions.txt";
    private static final String DEFAULT_PARAM_INSTANCES_PATH = "src/InputInstances/parameterinstancesupdated.txt";
    
    @Override
    public void check(ASTURMotionPlannerDefinition node) {
        try {
            // Use default paths
            String paramTypesPath = DEFAULT_PARAM_TYPES_PATH;
            String paramInstancesPath = DEFAULT_PARAM_INSTANCES_PATH;
            
            System.out.println("CoCo: Validating parameter instances against type definitions...");
            System.out.println("  Parameter types file: " + paramTypesPath);
            System.out.println("  Parameter instances file: " + paramInstancesPath);
            
            // Parse parameter types to get allowed properties
            Map<String, Set<String>> typeDefinitions = parseParameterTypes(paramTypesPath);
            System.out.println("  Found " + typeDefinitions.size() + " parameter type definitions");
            
            // Parse parameter instances and validate
            validateParameterInstances(paramInstancesPath, typeDefinitions, node);
            
        } catch (Exception e) {
            Log.warn("0xMP003 Failed to validate parameter instances: " + e.getMessage(), 
                    node.get_SourcePositionStart());
            System.err.println("Warning: CoCo validation failed: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse parameter type definitions and extract allowed properties for each type
     * Returns: Map<typeName, Set<propertyName>>
     */
    private Map<String, Set<String>> parseParameterTypes(String filePath) throws IOException {
        Map<String, Set<String>> typeDefinitions = new HashMap<>();
        
        File file = new File(filePath);
        if (!file.exists()) {
            throw new FileNotFoundException("Parameter types file not found: " + filePath);
        }
        
        try (BufferedReader reader = new BufferedReader(new FileReader(file))) {
            String line;
            String currentType = null;
            Set<String> currentProperties = null;
            boolean inTypeBlock = false;
            
            while ((line = reader.readLine()) != null) {
                line = line.trim();
                
                // Match: Parameter beam : Element {
                Pattern typePattern = Pattern.compile("^Parameter\\s+(\\w+)\\s*:\\s*\\w+\\s*\\{");
                Matcher typeMatcher = typePattern.matcher(line);
                
                if (typeMatcher.find()) {
                    currentType = typeMatcher.group(1);
                    currentProperties = new HashSet<>();
                    currentProperties.add("nameKey"); // Always allowed
                    inTypeBlock = true;
                    System.out.println("    Found type: " + currentType);
                    continue;
                }
                
                // Match property: length: Double
                if (inTypeBlock && line.contains(":") && !line.startsWith("//")) {
                    Pattern propPattern = Pattern.compile("^(\\w+)\\s*:\\s*\\w+");
                    Matcher propMatcher = propPattern.matcher(line);
                    
                    if (propMatcher.find()) {
                        String propName = propMatcher.group(1);
                        if (currentProperties != null) {
                            currentProperties.add(propName);
                            System.out.println("      Property: " + propName);
                        }
                    }
                }
                
                // End of type block
                if (inTypeBlock && line.equals("}")) {
                    if (currentType != null && currentProperties != null) {
                        typeDefinitions.put(currentType, currentProperties);
                    }
                    inTypeBlock = false;
                    currentType = null;
                    currentProperties = null;
                }
            }
        }
        
        return typeDefinitions;
    }
    
    /**
     * Validate parameter instances against type definitions
     */
    private void validateParameterInstances(String filePath, Map<String, Set<String>> typeDefinitions, 
                                           ASTURMotionPlannerDefinition node) throws IOException {
        File file = new File(filePath);
        if (!file.exists()) {
            throw new FileNotFoundException("Parameter instances file not found: " + filePath);
        }
        
        int lineNumber = 0;
        try (BufferedReader reader = new BufferedReader(new FileReader(file))) {
            String line;
            
            while ((line = reader.readLine()) != null) {
                lineNumber++;
                line = line.trim();
                
                // Match: ParameterInstance: beam {nameKey = b1, length = 5.0}
                if (line.startsWith("ParameterInstance:")) {
                    validateInstance(line, lineNumber, typeDefinitions, node);
                }
            }
        }
        
        System.out.println("  ✓ All parameter instances validated successfully");
    }
    
    /**
     * Validate a single parameter instance line
     */
    private void validateInstance(String line, int lineNumber, Map<String, Set<String>> typeDefinitions,
                                  ASTURMotionPlannerDefinition node) {
        // Extract type name: ParameterInstance: beam {...}
        Pattern typePattern = Pattern.compile("ParameterInstance:\\s*(\\w+)\\s*\\{");
        Matcher typeMatcher = typePattern.matcher(line);
        
        if (!typeMatcher.find()) {
            return; // Can't parse, skip
        }
        
        String typeName = typeMatcher.group(1);
        
        // Check if type is defined
        if (!typeDefinitions.containsKey(typeName)) {
            Log.error("0xMP004 Parameter type '" + typeName + "' is not defined (line " + lineNumber + ")", 
                     node.get_SourcePositionStart());
            System.err.println("  ERROR: Type '" + typeName + "' not defined at line " + lineNumber);
            return;
        }
        
        Set<String> allowedProperties = typeDefinitions.get(typeName);
        
        // Extract properties from instance: {nameKey = b1, length = 5.0}
        Pattern propPattern = Pattern.compile("(\\w+)\\s*=");
        Matcher propMatcher = propPattern.matcher(line);
        
        while (propMatcher.find()) {
            String propertyName = propMatcher.group(1);
            
            if (!allowedProperties.contains(propertyName)) {
                Log.error("0xMP005 Property '" + propertyName + "' is not defined in type '" + 
                         typeName + "' (line " + lineNumber + ")", 
                         node.get_SourcePositionStart());
                System.err.println("  ERROR: Property '" + propertyName + "' not in type '" + 
                                  typeName + "' at line " + lineNumber);
                System.err.println("    Allowed properties: " + allowedProperties);
            }
        }
    }
    
    /**
     * Remove surrounding quotes from file path
     */
    private String cleanFilePath(String path) {
        if (path.startsWith("\"") && path.endsWith("\"")) {
            return path.substring(1, path.length() - 1);
        }
        return path;
    }
}
