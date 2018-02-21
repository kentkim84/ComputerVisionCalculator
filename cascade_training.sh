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
PRE_DIR="${r#/home/ubuntu/opencv_workplace/}"
NEW_DIR="${PRE_DIR%.*}"
echo "Process directory with an image file: $r"
echo
echo -e "\e[31;43m***** PREPARE FILE AND FOLDER *****\e[0m"
if [ -r ~/opencv_workplace/data ]; then
mv ~/opencv_workplace/data ~/opencv_workplace/resources/"$NEW_DIR"
echo "data folder moved to resource folder"
fi
if [ -r ~/opencv_workplace/info ]; then
mv ~/opencv_workplace/info ~/opencv_workplace/resources/"$NEW_DIR"
echo "info folder moved to resource folder"
fi
if [ -r ~/opencv_workplace/*.vec ]; then
mv ~/opencv_workplace/*.vec ~/opencv_workplace/resources/"$NEW_DIR"
echo "positive.vec file moved to resource folder"
fi
echo "Preparation Finished"
echo
# Phase 1: Create positive images
echo -e "\e[31;43m***** CREATE POSITIVE IMAGES *****\e[0m"
# - Create the 'info' folder then generate positive images
## only test purpose ##
mkdir ~/opencv_workplace/info
# Phase 2: Create vertor a vector file
echo -e "\e[31;43m***** CREATE A VERTOR FILE *****\e[0m"
echo "Vector file created"
# Phase 3: Start train
echo -e "\e[31;43m***** TRAIN AND CREATE CASCADE CLASSIFIER *****\e[0m"
# - Create the 'data' folder for the next positive source
mkdir ~/opencv_workplace/data
echo "Train finished"
done
