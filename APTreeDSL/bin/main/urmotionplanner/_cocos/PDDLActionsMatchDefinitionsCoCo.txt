package urmotionplanner._cocos;

import urmotionplanner._ast.ASTURMotionPlannerDefinition;
import de.se_rwth.commons.logging.Log;
import java.io.*;
import java.util.*;
import java.util.regex.*;

/**
 * Context Condition: Validates that all actions defined in PDDL domain file
 * have corresponding action type definitions.
 */
public class PDDLActionsMatchDefinitionsCoCo implements URMotionPlannerASTURMotionPlannerDefinitionCoCo {
    
    // Default paths
    private static final String DEFAULT_PDDL_DOMAINS_DIR = "python_service/Plannerinputs";
    private static final String DEFAULT_ACTION_TYPES_PATH = "src/test/resources/valid/crf/PDDLActions.txt";
    
    @Override
    public void check(ASTURMotionPlannerDefinition node) {
        try {
            // Use default paths
            String pddlDomainsDir = DEFAULT_PDDL_DOMAINS_DIR;
            String actionTypesPath = DEFAULT_ACTION_TYPES_PATH;
            
            System.out.println("CoCo: Validating PDDL domain actions against type definitions...");
            System.out.println("  PDDL domains directory: " + pddlDomainsDir);
            System.out.println("  Action types file: " + actionTypesPath);
            
            // Extract action names from all PDDL domain files
            Set<String> pddlActions = extractPDDLActionNamesFromDirectory(pddlDomainsDir);
            System.out.println("  Found " + pddlActions.size() + " unique actions across all PDDL domains");
            
            // Extract defined action types
            Set<String> definedActions = extractActionTypeNames(actionTypesPath);
            System.out.println("  Found " + definedActions.size() + " action type definitions");
            
            // Check if all PDDL actions have definitions
            Set<String> missingActions = new HashSet<>();
            for (String pddlAction : pddlActions) {
                if (!definedActions.contains(pddlAction)) {
                    missingActions.add(pddlAction);
                }
            }
            
            // Report errors for missing action definitions
            if (!missingActions.isEmpty()) {
                for (String missingAction : missingActions) {
                    Log.error("0xMP009 PDDL action '" + missingAction + "' has no corresponding action type definition", 
                             node.get_SourcePositionStart());
                }
                System.err.println("ERROR: " + missingActions.size() + " PDDL actions without definitions: " + missingActions);
            } else {
                System.out.println("  ✓ All PDDL actions have corresponding type definitions");
            }
            
        } catch (Exception e) {
            Log.warn("0xMP010 Failed to validate PDDL actions: " + e.getMessage(), 
                    node.get_SourcePositionStart());
            System.err.println("Warning: PDDL action CoCo validation failed: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Extract action names from all PDDL domain files in a directory
     */
    private Set<String> extractPDDLActionNamesFromDirectory(String directoryPath) throws IOException {
        Set<String> allActions = new HashSet<>();
        
        File directory = new File(directoryPath);
        if (!directory.exists() || !directory.isDirectory()) {
            throw new FileNotFoundException("PDDL domains directory not found: " + directoryPath);
        }
        
        // Find all .pddl files in directory
        File[] pddlFiles = directory.listFiles((dir, name) -> name.toLowerCase().endsWith(".pddl"));
        
        if (pddlFiles == null || pddlFiles.length == 0) {
            System.out.println("    Warning: No PDDL files found in " + directoryPath);
            return allActions;
        }
        
        System.out.println("    Scanning " + pddlFiles.length + " PDDL files...");
        
        // Process each PDDL file
        for (File pddlFile : pddlFiles) {
            // Check if it's a domain file (not a problem file)
            if (isDomainFile(pddlFile)) {
                System.out.println("    Processing domain file: " + pddlFile.getName());
                Set<String> actions = extractPDDLActionNames(pddlFile.getAbsolutePath());
                allActions.addAll(actions);
            }
        }
        
        return allActions;
    }
    
    /**
     * Check if a PDDL file is a domain file (not a problem file)
     */
    private boolean isDomainFile(File pddlFile) throws IOException {
        try (BufferedReader reader = new BufferedReader(new FileReader(pddlFile))) {
            String line;
            while ((line = reader.readLine()) != null) {
                if (line.contains("(domain ") || line.contains("(define (domain")) {
                    return true;
                }
                // Problem files have (problem ...)
                if (line.contains("(problem ")) {
                    return false;
                }
            }
        }
        return false; // If unclear, skip
    }
    
    /**
     * Extract action names from a single PDDL domain file
     * Looks for: (:action action_name ...)
     */
    private Set<String> extractPDDLActionNames(String pddlPath) throws IOException {
        Set<String> actionNames = new HashSet<>();
        
        File pddlFile = new File(pddlPath);
        if (!pddlFile.exists()) {
            throw new FileNotFoundException("PDDL domain file not found: " + pddlPath);
        }
        
        try (BufferedReader reader = new BufferedReader(new FileReader(pddlFile))) {
            String line;
            
            while ((line = reader.readLine()) != null) {
                line = line.trim();
                
                // Match: (:action pickup or (:action pick-up or (:action PICKUP
                Pattern actionPattern = Pattern.compile("\\(:action\\s+([\\w-]+)", Pattern.CASE_INSENSITIVE);
                Matcher actionMatcher = actionPattern.matcher(line);
                
                if (actionMatcher.find()) {
                    String actionName = actionMatcher.group(1);
                    // Normalize: convert hyphens to camelCase or just use lowercase
                    actionName = normalizeActionName(actionName);
                    actionNames.add(actionName);
                    System.out.println("    Found PDDL action: " + actionName);
                }
            }
        }
        
        return actionNames;
    }
    
    /**
     * Extract action type names from action definitions file
     * Looks for: Action pickup { or Action equipe {
     */
    private Set<String> extractActionTypeNames(String actionTypesPath) throws IOException {
        Set<String> actionNames = new HashSet<>();
        
        File file = new File(actionTypesPath);
        if (!file.exists()) {
            throw new FileNotFoundException("Action types file not found: " + actionTypesPath);
        }
        
        try (BufferedReader reader = new BufferedReader(new FileReader(file))) {
            String line;
            
            while ((line = reader.readLine()) != null) {
                line = line.trim();
                
                // Match: Action pickup {
                Pattern actionPattern = Pattern.compile("^Action\\s+(\\w+)\\s*\\{");
                Matcher actionMatcher = actionPattern.matcher(line);
                
                if (actionMatcher.find()) {
                    String actionName = actionMatcher.group(1).toLowerCase();
                    actionNames.add(actionName);
                    System.out.println("    Found action type definition: " + actionName);
                }
            }
        }
        
        return actionNames;
    }
    
    /**
     * Normalize PDDL action name to match definition format
     * Converts: pick-up -> pickup, PICKUP -> pickup
     */
    private String normalizeActionName(String actionName) {
        return actionName.toLowerCase().replaceAll("-", "");
    }
}
