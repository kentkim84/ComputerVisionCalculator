import cv2
import numpy as np

face_cascade = cv2.CascadeClassifier('haarcascade_frontalface_default.xml')
eye_cascade = cv2.CascadeClassifier('haarcascade_eye.xml')
addition_cascade = cv2.CascadeClassifier('haarcascade_addition.xml')
subtraction_cascade = cv2.CascadeClassifier('haarcascade_subtraction.xml')
multiplication_cascade = cv2.CascadeClassifier('haarcascade_multiplication.xml')
division_cascade = cv2.CascadeClassifier('haarcascade_division.xml')

img_bgr = cv2.imread('sample.jpg')
img_gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)

additons = addition_cascade.detectMultiScale(img_gray, 1.2, 5)
for (ax,ay,aw,ah) in additons:    
    cv2.rectangle(img_bgr,(ax,ay),(ax+aw,ay+ah),(255,0,0),2)

subtractions = subtraction_cascade.detectMultiScale(img_gray, 1.2, 5)
for (bx,by,bw,bh) in subtractions:
    cv2.rectangle(img_bgr,(bx,by),(bx+bw,by+bh),(0,255,0),2)

multiplications = multiplication_cascade.detectMultiScale(img_gray, 1.2, 5)
for (cx,cy,cw,ch) in multiplications:
    cv2.rectangle(img_bgr,(cx,cy),(cx+cw,cy+ch),(0,0,255),2)

divisions = division_cascade.detectMultiScale(img_gray, 1.2, 5)
for (dx,dy,dw,dh) in divisions:
    cv2.rectangle(img_bgr,(dx,dy),(dx+dw,dy+dh),(255,255,0),2)


faces = face_cascade.detectMultiScale(img_gray, 1.3, 3)
for (x,y,w,h) in faces:
    cv2.rectangle(img_bgr, (x,y),(x+w,y+h),(255,0,0),2)
    roi_gray = img_gray[y:y+h, x:x+w]
    roi_color = img_bgr[y:y+h, x:x+w]
    eyes = eye_cascade.detectMultiScale(roi_gray)
    for (ex,ey,ew,eh) in eyes:
        cv2.rectangle(roi_color, (ex,ey),(ex+ew,ey+eh),(0,255,0),2)

cv2.imshow('detected', img_bgr)
cv2.waitKey(0)
cv2.destroyAllWindows()
