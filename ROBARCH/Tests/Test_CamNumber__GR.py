#______________________________________________________________________
#______________________________________________________________________	
# Run the script.
# For each camera index that’s available, a window will pop up showing its feed.
# Press q in the window to close it and move to the next camera.
# Note which index corresponds to your built-in webcam, external USB camera, depth cam, etc.
#______________________________________________________________________
#______________________________________________________________________	

import cv2

for i in range(5):  # check first 5 devices
    cap = cv2.VideoCapture(i) #checking for cam number on my laptop!
    if cap.isOpened():
        print(f"Camera {i} is available – showing preview. Press 'q' to close.")
        while True:
            ret, frame = cap.read()
            if not ret:
                break
            cv2.imshow(f"Camera {i}", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
        cap.release()
        cv2.destroyAllWindows()
    else:
        print(f"Camera {i} not found")
