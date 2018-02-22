#!/bin/bash
# Automated Script written for executing cascade classifier training sequences.
# This script will return the following set of cascade classifier.s
# -Variables-
RESOURCES=(~/opencv_workplace/*.jpg)
# -Process all image files in an array
for r in "${RESOURCES[@]}"
do
# -Drop Caches and Free memory-
echo -e "\e[31;43m***** DROP CACHES AND FREE MEMORY *****\e[0m"
sudo sh -c '/bin/echo 1 > /proc/sys/vm/drop_caches'
sudo sh -c '/bin/echo 2 > /proc/sys/vm/drop_caches'
sudo sh -c '/bin/echo 3 > /proc/sys/vm/drop_caches'
free
echo 
# -Preparations-
IMG_NAME="${r#/home/ubuntu/opencv_workplace/}"
NEW_DIR="${IMG_NAME%.*}"
echo "Process directory with an image file: $r"
echo "IMG_NAME: $IMG_NAME"
echo "NEW_DIR: $NEW_DIR"
echo
echo -e "\e[31;43m***** CLEANUP FILE AND FOLDER *****\e[0m"
if [ -r ~/opencv_workplace/data ]; then
mv ~/opencv_workplace/data ~/opencv_workplace/resources/"$NEW_DIR"
echo "Data folder moved to resource folder"
else
echo "Data folder doesn't exist"
fi
if [ -r ~/opencv_workplace/info ]; then
mv ~/opencv_workplace/info ~/opencv_workplace/resources/"$NEW_DIR"
echo "Info folder moved to resource folder"
else
echo "Info folder doesn't exist"
fi
if [ -r ~/opencv_workplace/*.vec ]; then
mv ~/opencv_workplace/*.vec ~/opencv_workplace/resources/"$NEW_DIR"
echo "Vector file moved to resource folder"
else
echo "Vector file doesn't exist"
fi
echo "CLEANUP Finished"
echo
# Phase 1: Create positive images
echo -e "\e[31;43m***** CREATE POSITIVE IMAGES *****\e[0m"
# - Create the 'info' folder then generate positive images
## only test purpose ##
## mkdir ~/opencv_workplace/info
opencv_createsamples -img ~/opencv_workplace/"$IMG_NAME" -bg ~/opencv_workplace/bg.txt -info ~/opencv_workplace/info/info.lst -pngoutput info -maxxangle 0.5 -maxyangle 0.5 -maxzangle 0.5 -num 2000
echo "Info folder created"
# Phase 2: Create vertor a vector file
echo -e "\e[31;43m***** CREATE A VERTOR FILE *****\e[0m"
## only test purpose ##
## echo "test" >> ~/opencv_workplace/test.vec
opencv_createsamples -info ~/opencv_workplace/info/info.lst -num 2000 -w 20 -h 20 -vec ~/opencv_workplace/positives.vec
echo "Vector file created"
# Phase 3: Start train
echo -e "\e[31;43m***** TRAIN AND CREATE CASCADE CLASSIFIER *****\e[0m"
# - Create the 'data' folder for the next positive source
mkdir ~/opencv_workplace/data
echo "Data folder created"
echo "Train Start"
opencv_traincascade -data ~/opencv_workplace/data -vec ~/opencv_workplace/positives.vec -bg ~/opencv_workplace/bg.txt -numPos 1800 -numNeg 900 -numStages 10 -w 20 -h 20
echo "Train finished"
# Phase 4: Move trained data
if [ -r ~/opencv_workplace/data ]; then
mv ~/opencv_workplace/data ~/opencv_workplace/resources/"$NEW_DIR"
echo "Data folder moved to resource folder"
else
echo "Data folder doesn't exist"
fi
if [ -r ~/opencv_workplace/info ]; then
mv ~/opencv_workplace/info ~/opencv_workplace/resources/"$NEW_DIR"
echo "Info folder moved to resource folder"
else
echo "Info folder doesn't exist"
fi
if [ -r ~/opencv_workplace/*.vec ]; then
mv ~/opencv_workplace/*.vec ~/opencv_workplace/resources/"$NEW_DIR"
echo "Vector file moved to resource folder"
else
echo "Vector file doesn't exist"
fi
done
