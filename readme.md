## Purpose
This document provides an overview of the Final Year Project for Bachelor of Science in Software Development program, including a statement of goals and objectives and a general description of my approach. This working document is directed to my supervisors but should be useful to anyone interested in learning more about creating and training own Haar Cascade Classifiers. This document is something of an executive summary of the project in its current state and summarise the results in phases.

## Goals and Objectives
My primary mission is to comprehend and develop an implementation that detects multiple objects using Haar Feature-based Cascade Classifiers.
Furthermore, this implementation can be extended to any objects as Haar Feature-based Cascade Classifiers are trained.
To achieve this overall objective, I have defined following phases.
* Phase 1: Gathering positive and negative samples.
    1. Collect negative and positive images from the web.
    2. Search and remove unnecessary images.
* Phase 2: Create a background info text file that will be used to create samples and train
* Phase 3: Generate own Haar Feature-based Cascade Classifiers.
    1. Create positive samples, based on a positive image/s
    2. Create a vector file, which is basically where all of positive images stitched together.
    3. Train own Haar Feature-based Cascade Classifiers.
* Phase 4: Implement own Haar Feature-based Cascade Classifiers to an application that uses both a camera and an image source file.
* Phase 5: Detect multiple objects from a captured image or an image file.
* Phase 6: Recognise objects what they are.
* Phase 7: Process them based on what are recognised.
* Phase 8: Display the result.

## Challenges and Constraints
### Apperances of incorrect fragments of nagative and positive images.
As selection of images made by humans. Question still remains how much we can trust humans' decision of selection.
### Inaccurate cascade classifiers trained.
Small fractions of inaccurate images would make significant imprecise cascade classifiers.
### Positive samples

## Running the tests
### Clone
Create a folder, and move into the folder just created, then use git clone command to download a copy of this project
```
$ mkdir folder
$ cd folder
$ git clone https://github.com/kentkim84/ComputerVisionImageAnalysis.git
```
### Download
Click [Clone or Download](https://github.com/kentkim84/ComputerVisionImageAnalysis), then Click 'Download ZIP'

### Test
MySql must be running.
Open the project with a suitable IDE.
Run the application as Spring Boot App.

## Authors
* **Yongjin Kim** - *Initial work* - [Kentkim84](https://github.com/kentkim84)

## References
* Original Project written by [Gerard Harrison](Gerard.Harrison@gmit.ie)
* [Microsoft Azure Computer Vision](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/)
* [Microsoft Azure Bing Image Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-image-search/)
* [Microsoft Universal Windows Platform](https://docs.microsoft.com/en-us/windows/uwp/index)