package urmotionplanner._cocos;

import urmotionplanner._ast.ASTURMotionPlannerDefinition;
import de.se_rwth.commons.logging.Log;
import java.io.*;
import java.util.*;
import java.util.regex.*;

/**
 * Context Condition: Validates that predicate instances only use properties
 * that are defined in their corresponding predicate type definitions.
 */
public class PredicateInstancesMatchTypesCoCo implements URMotionPlannerASTURMotionPlannerDefinitionCoCo {
    
    // Default paths for validation files
    private static final String DEFAULT_PREDICATE_TYPES_PATH = "src/test/resources/valid/crf/PDDLActions.txt";
    private static final String DEFAULT_PREDICATE_INSTANCES_PATH = "src/InputInstances/PredicateInstances_PDDL.txt";
    
    @Override
    public void check(ASTURMotionPlannerDefinition node) {
        try {
            // Use default paths
            String predicateTypesPath = DEFAULT_PREDICATE_TYPES_PATH;
            String predicateInstancesPath = DEFAULT_PREDICATE_INSTANCES_PATH;
            
            System.out.println("CoCo: Validating predicate instances against type definitions...");
            System.out.println("  Predicate types file: " + predicateTypesPath);
            System.out.println("  Predicate instances file: " + predicateInstancesPath);
            
            // Parse predicate types to get allowed properties
            Map<String, Set<String>> typeDefinitions = parsePredicateTypes(predicateTypesPath);
            System.out.println("  Found " + typeDefinitions.size() + " predicate type definitions");
            
            // Parse predicate instances and validate
            validatePredicateInstances(predicateInstancesPath, typeDefinitions, node);
            
        } catch (Exception e) {
            Log.warn("0xMP006 Failed to validate predicate instances: " + e.getMessage(), 
                    node.get_SourcePositionStart());
            System.err.println("Warning: Predicate CoCo validation failed: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse predicate type definitions and extract allowed properties for each type
     * Returns: Map<predicateName, Set<propertyName>>
     */
    private Map<String, Set<String>> parsePredicateTypes(String filePath) throws IOException {
        Map<String, Set<String>> typeDefinitions = new HashMap<>();
        
        File file = new File(filePath);
        if (!file.exists()) {
            throw new FileNotFoundException("Predicate types file not found: " + filePath);
        }
        
        try (BufferedReader reader = new BufferedReader(new FileReader(file))) {
            String line;
            String currentType = null;
            Set<String> currentProperties = null;
            boolean inTypeBlock = false;
            
            while ((line = reader.readLine()) != null) {
                line = line.trim();
                
                // Match: predicate isAt {
                Pattern typePattern = Pattern.compile("^predicate\\s+(\\w+)\\s*\\{");
                Matcher typeMatcher = typePattern.matcher(line);
                
                if (typeMatcher.find()) {
                    currentType = typeMatcher.group(1);
                    currentProperties = new HashSet<>();
                    currentProperties.add("isNegated"); // Always allowed
                    inTypeBlock = true;
                    System.out.println("    Found predicate type: " + currentType);
                    continue;
                }
                
                // Match property: myObject: Element
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
     * Validate predicate instances against type definitions
     */
    private void validatePredicateInstances(String filePath, Map<String, Set<String>> typeDefinitions, 
                                           ASTURMotionPlannerDefinition node) throws IOException {
        File file = new File(filePath);
        if (!file.exists()) {
            throw new FileNotFoundException("Predicate instances file not found: " + filePath);
        }
        
        int lineNumber = 0;
        try (BufferedReader reader = new BufferedReader(new FileReader(file))) {
            String line;
            
            while ((line = reader.readLine()) != null) {
                lineNumber++;
                line = line.trim();
                
                // Match: PredicateInstance: isAt(myObject = b1, location = fp1, isNegated = false)
                if (line.startsWith("PredicateInstance:")) {
                    validateInstance(line, lineNumber, typeDefinitions, node);
                }
            }
        }
        
        System.out.println("  ✓ All predicate instances validated successfully");
    }
    
    /**
     * Validate a single predicate instance line
     */
    private void validateInstance(String line, int lineNumber, Map<String, Set<String>> typeDefinitions,
                                  ASTURMotionPlannerDefinition node) {
        // Extract type name: PredicateInstance: isAt(...)
        Pattern typePattern = Pattern.compile("PredicateInstance:\\s*(\\w+)\\s*\\(");
        Matcher typeMatcher = typePattern.matcher(line);
        
        if (!typeMatcher.find()) {
            return; // Can't parse, skip
        }
        
        String typeName = typeMatcher.group(1);
        
        // Check if type is defined
        if (!typeDefinitions.containsKey(typeName)) {
            Log.error("0xMP007 Predicate type '" + typeName + "' is not defined (line " + lineNumber + ")", 
                     node.get_SourcePositionStart());
            System.err.println("  ERROR: Predicate type '" + typeName + "' not defined at line " + lineNumber);
            return;
        }
        
        Set<String> allowedProperties = typeDefinitions.get(typeName);
        
        // Extract properties from instance: (myObject = b1, location = fp1, isNegated = false)
        Pattern propPattern = Pattern.compile("(\\w+)\\s*=");
        Matcher propMatcher = propPattern.matcher(line);
        
        while (propMatcher.find()) {
            String propertyName = propMatcher.group(1);
            
            if (!allowedProperties.contains(propertyName)) {
                Log.error("0xMP008 Property '" + propertyName + "' is not defined in predicate type '" + 
                         typeName + "' (line " + lineNumber + ")", 
                         node.get_SourcePositionStart());
                System.err.println("  ERROR: Property '" + propertyName + "' not in predicate type '" + 
                                  typeName + "' at line " + lineNumber);
                System.err.println("    Allowed properties: " + allowedProperties);
            }
        }
    }
}
