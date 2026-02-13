package urmotionplanner._cocos;

import urmotionplanner._ast.ASTURMotionPlannerDefinition;
import de.se_rwth.commons.logging.Log;
import java.io.*;
import java.util.*;
import javax.xml.parsers.*;
import org.w3c.dom.*;

/**
 * Context Condition: Validates that all objects in the Collada file
 * have corresponding parameter instances defined.
 */
public class ColladaObjectsMatchParametersCoCo implements URMotionPlannerASTURMotionPlannerDefinitionCoCo {
    
    // Default path for parameter instances
    private static final String DEFAULT_PARAM_INSTANCES_PATH = "src/InputInstances/parameterinstancesupdated.txt";
    
    @Override
    public void check(ASTURMotionPlannerDefinition node) {
        try {
            // Get collada path from definition, use default for parameter instances
            String colladaPath = cleanFilePath(node.getFilesSection().getColladaAssignment().getFilePath().getSTRING_VALUE());
            String paramInstancesPath = DEFAULT_PARAM_INSTANCES_PATH;
            
            System.out.println("CoCo: Validating Collada objects against parameters...");
            System.out.println("  Collada file: " + colladaPath);
            System.out.println("  Parameter instances file: " + paramInstancesPath);
            
            // Extract object names from Collada file
            Set<String> colladaObjects = extractColladaObjectNames(colladaPath);
            System.out.println("  Found " + colladaObjects.size() + " objects in Collada file");
            
            // Extract parameter instances from input file
            Set<String> definedParameters = extractParameterInstanceNames(paramInstancesPath);
            System.out.println("  Found " + definedParameters.size() + " defined parameters");
            
            // Check if all Collada objects have corresponding parameters
            Set<String> unmatchedObjects = new HashSet<>();
            for (String colladaObj : colladaObjects) {
                if (!definedParameters.contains(colladaObj)) {
                    unmatchedObjects.add(colladaObj);
                }
            }
            
            // Report errors for unmatched objects
            if (!unmatchedObjects.isEmpty()) {
                for (String unmatchedObj : unmatchedObjects) {
                    Log.error("0xMP001 Object '" + unmatchedObj + "' found in Collada file but not defined in parameter instances", 
                             node.get_SourcePositionStart());
                }
                System.err.println("ERROR: " + unmatchedObjects.size() + " unmatched objects found: " + unmatchedObjects);
            } else {
                System.out.println("  ✓ All Collada objects have corresponding parameter definitions");
            }
            
        } catch (Exception e) {
            Log.warn("0xMP002 Failed to validate Collada objects: " + e.getMessage(), 
                    node.get_SourcePositionStart());
            System.err.println("Warning: CoCo validation failed: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Extract object names from Collada (DAE) XML file
     */
    private Set<String> extractColladaObjectNames(String colladaPath) throws Exception {
        Set<String> objectNames = new HashSet<>();
        
        File colladaFile = new File(colladaPath);
        if (!colladaFile.exists()) {
            throw new FileNotFoundException("Collada file not found: " + colladaPath);
        }
        
        // Parse the Collada XML file
        DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
        DocumentBuilder builder = factory.newDocumentBuilder();
        Document doc = builder.parse(colladaFile);
        
        // Extract node elements (geometry/visual scene nodes)
        // Collada typically has <node> elements with id/name attributes
        NodeList nodes = doc.getElementsByTagName("node");
        for (int i = 0; i < nodes.getLength(); i++) {
            Element nodeElement = (Element) nodes.item(i);
            
            // Try to get name or id attribute
            String nodeName = null;
            if (nodeElement.hasAttribute("name")) {
                nodeName = nodeElement.getAttribute("name");
            } else if (nodeElement.hasAttribute("id")) {
                nodeName = nodeElement.getAttribute("id");
            }
            
            if (nodeName != null && !nodeName.isEmpty()) {
                // Extract the base name (e.g., "b1" from "beam_b1" or just "b1")
                String baseName = extractBaseName(nodeName);
                objectNames.add(baseName);
                System.out.println("    Found object: " + baseName + " (from: " + nodeName + ")");
            }
        }
        
        return objectNames;
    }
    
    /**
     * Extract parameter instance names from the parameter instances file
     */
    private Set<String> extractParameterInstanceNames(String paramInstancesPath) throws Exception {
        Set<String> parameterNames = new HashSet<>();
        
        File paramFile = new File(paramInstancesPath);
        if (!paramFile.exists()) {
            throw new FileNotFoundException("Parameter instances file not found: " + paramInstancesPath);
        }
        
        try (BufferedReader reader = new BufferedReader(new FileReader(paramFile))) {
            String line;
            while ((line = reader.readLine()) != null) {
                line = line.trim();
                
                // Look for lines like: ParameterInstance: beam {nameKey = b1}
                if (line.startsWith("ParameterInstance:")) {
                    String nameKey = extractNameKey(line);
                    if (nameKey != null) {
                        parameterNames.add(nameKey);
                        System.out.println("    Found parameter: " + nameKey);
                    }
                }
            }
        }
        
        return parameterNames;
    }
    
    /**
     * Extract base name from object name (e.g., "b1" from "beam_b1" or "Beam_b1_mesh")
     */
    private String extractBaseName(String objectName) {
        // Common patterns: beam_b1, b1, Beam_b1_mesh, etc.
        // Try to extract the simplest identifier (like b1, lp1, etc.)
        
        // Remove common suffixes
        objectName = objectName.replaceAll("_mesh$", "")
                               .replaceAll("_visual$", "")
                               .replaceAll("_collision$", "");
        
        // If it contains underscore, take the last part (usually the ID)
        if (objectName.contains("_")) {
            String[] parts = objectName.split("_");
            objectName = parts[parts.length - 1];
        }
        
        return objectName.toLowerCase();
    }
    
    /**
     * Extract nameKey value from parameter instance line
     * Example: "ParameterInstance: beam {nameKey = b1}" -> "b1"
     */
    private String extractNameKey(String line) {
        // Look for {nameKey = XXX}
        int startIdx = line.indexOf("nameKey");
        if (startIdx < 0) return null;
        
        int equalsIdx = line.indexOf("=", startIdx);
        if (equalsIdx < 0) return null;
        
        int endIdx = line.indexOf("}", equalsIdx);
        if (endIdx < 0) return null;
        
        String nameKey = line.substring(equalsIdx + 1, endIdx).trim();
        return nameKey;
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
