import urmotionplanner._parser.URMotionPlannerParser;
import urmotionplanner._ast.ASTURMotionPlannerDefinition;
import urmotionplanner._ast.ASTFilesSection;
import urmotionplanner._ast.ASTSettingsSection;
import urmotionplanner._ast.ASTColladaAssignment;
import urmotionplanner._ast.ASTStlAssignment;
import urmotionplanner._ast.ASTWorkspaceAssignment;
import urmotionplanner._ast.ASTPlannerNameAssignment;
import urmotionplanner._ast.ASTPlannerType;
import urmotionplanner._ast.ASTURTypeAssignment;
import urmotionplanner._ast.ASTURType;
import urmotionplanner._cocos.URMotionPlannerCoCoChecker;
import de.se_rwth.commons.logging.Log;
import java.util.Optional;
import java.io.*;

/**
 * Test class for UR Motion Planner grammar parsing with Context Conditions
 */
public class URMotionPlannerTest {
    
    public static void main(String[] args) {
        try {
            System.out.println("=== UR MOTION PLANNER PARSER TEST ===");
            System.out.println("Parsing motionplanner.txt...");
            
            // Define the file to parse
            String filePath = "src/test/resources/valid/Planners/motionplanner.txt";
            
            // Check if file exists
            File testFile = new File(filePath);
            if (!testFile.exists()) {
                System.err.println("ERROR: Test file not found: " + filePath);
                System.err.println("Current working directory: " + System.getProperty("user.dir"));
                return;
            }
            
            // Create parser instance
            URMotionPlannerParser parser = new URMotionPlannerParser();
            
            // Parse the file
            Optional<ASTURMotionPlannerDefinition> result = parser.parse(filePath);
            
            if (result.isPresent()) {
                ASTURMotionPlannerDefinition plannerDef = result.get();
                System.out.println("SUCCESS: Parsed UR Motion planner: " + plannerDef.getName());
                
                // Analyze the parsed planner definition
                analyzeMotionPlannerDefinition(plannerDef);
                
                // Run Context Conditions
                System.out.println("\n=== RUNNING CONTEXT CONDITIONS ===");
                URMotionPlannerCoCoChecker cocoChecker = URMotionPlannerCoCoChecker.getCheckerForAllCoCos();
                cocoChecker.checkAll(plannerDef);
                
                // Check for errors
                if (Log.getErrorCount() > 0) {
                    System.err.println("\n❌ VALIDATION FAILED: " + Log.getErrorCount() + " error(s) found");
                } else {
                    System.out.println("\n✓ VALIDATION PASSED: All context conditions satisfied");
                }
                
            } else {
                System.out.println("ERROR: Failed to parse " + filePath);
                System.out.println("Please check the grammar and test file for syntax errors.");
            }
            
        } catch (Exception e) {
            System.err.println("ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Analyze the parsed Motion planner definition
     */
    public static void analyzeMotionPlannerDefinition(ASTURMotionPlannerDefinition plannerDef) {
        System.out.println("\n=== UR MOTION PLANNER ANALYSIS ===");
        System.out.println("Planner Name: " + plannerDef.getName());
        
        // Analyze files section
        ASTFilesSection filesSection = plannerDef.getFilesSection();
        System.out.println("\n📁 Files Section:");
        
        // Get Collada file
        if (filesSection.getColladaAssignment() != null) {
            ASTColladaAssignment colladaAssignment = filesSection.getColladaAssignment();
            System.out.println("  Collada File: " + colladaAssignment.getFilePath().getSTRING_VALUE());
        }
        
        // Get STL file
        if (filesSection.getStlAssignment() != null) {
            ASTStlAssignment stlAssignment = filesSection.getStlAssignment();
            System.out.println("  STL File: " + stlAssignment.getFilePath().getSTRING_VALUE());
        }
        
        // Get Workspace (optional)
        if (filesSection.isPresentWorkspaceAssignment()) {
            ASTWorkspaceAssignment workspaceAssignment = filesSection.getWorkspaceAssignment();
            System.out.println("  Workspace: " + workspaceAssignment.getFilePath().getSTRING_VALUE());
        } else {
            System.out.println("  Workspace: (not specified)");
        }
        
        // Analyze settings section
        ASTSettingsSection settingsSection = plannerDef.getSettingsSection();
        System.out.println("\n⚙️ Settings Section:");
        
        if (settingsSection.getSettingAssignmentList() != null && !settingsSection.getSettingAssignmentList().isEmpty()) {
            for (urmotionplanner._ast.ASTSettingAssignment settingAssignment : settingsSection.getSettingAssignmentList()) {
                // Check for planner name
                if (settingAssignment.isPresentPlannerNameAssignment()) {
                    ASTPlannerNameAssignment plannerNameAssignment = settingAssignment.getPlannerNameAssignment();
                    ASTPlannerType plannerType = plannerNameAssignment.getPlannerType();
                    
                    String plannerName = "unknown";
                    if (plannerType.isPresentOmpl()) {
                        plannerName = "OMPL";
                    } else if (plannerType.isPresentStomp()) {
                        plannerName = "STOMP";
                    } else if (plannerType.isPresentChomp()) {
                        plannerName = "CHOMP";
                    } else if (plannerType.isPresentSbpl()) {
                        plannerName = "SBPL";
                    } else if (plannerType.isPresentPilz()) {
                        plannerName = "Pilz";
                    }
                    
                    System.out.println("  Planner Type: " + plannerName);
                }
                
                // Check for UR type
                if (settingAssignment.isPresentURTypeAssignment()) {
                    ASTURTypeAssignment urTypeAssignment = settingAssignment.getURTypeAssignment();
                    ASTURType urType = urTypeAssignment.getURType();
                    
                    String robotType = "unknown";
                    if (urType.isPresentUr3()) {
                        robotType = "UR3";
                    } else if (urType.isPresentUr3e()) {
                        robotType = "UR3e";
                    } else if (urType.isPresentUr5()) {
                        robotType = "UR5";
                    } else if (urType.isPresentUr5e()) {
                        robotType = "UR5e";
                    } else if (urType.isPresentUr10()) {
                        robotType = "UR10";
                    } else if (urType.isPresentUr10e()) {
                        robotType = "UR10e";
                    } else if (urType.isPresentUr16e()) {
                        robotType = "UR16e";
                    } else if (urType.isPresentUr20()) {
                        robotType = "UR20";
                    } else if (urType.isPresentUr30()) {
                        robotType = "UR30";
                    }
                    
                    System.out.println("  Robot Type: " + robotType);
                }
            }
        }
        
        System.out.println("\n=== PARSING COMPLETE ===");
    }
}
