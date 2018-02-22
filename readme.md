## Purpose
This document provides an overview of the Final Year Project for Bachelor of Science in Software Development program, including a statement of goals and objectives and a general description of my approach. This working document is directed to my supervisors but should be useful to anyone interested in learning more about this project. This document is something of an executive summary of the project in its current state and summarise the results in phases.

## Goals and Objectives
My primary mission is to comprehend and develop an implementation that detects mathematical symbols and numbers using Haar Feature-based Cascade Classifiers.
Furthermore, this implementation can be extended to any objects as Haar Feature-based Cascade Classifiers are trained.
To achieve this overall objective, I have defined following phases.
* Phase 1: Gathering positive and negative images.
    1. Download negative and positive images.
    2. Find and remove unnecessary images.
* Phase 2: Create a background info text file that will be used to create samples and train
* Phase 3: Generate own Haar Feature-based Cascade Classifiers.
    1. Create positive samples, based on a positive image/s
    2. Create the vector file, which is basically where all of positive images stitched together.
    3. Train own Haar Feature-based Cascade Classifiers.
* Phase 4: Implement own Haar Feature-based Cascade Classifiers to an app that uses both a camera and an image file.
* Phase 5: Detect multiple objects from a source (camera or image file).
* Phase 6: Recognise numbers and mathematical symbol.
* Phase 7: Calculate based on what are recognised.
* Phase 8: Display the result.

## Challenges and Constraints
### Apperances of incorrect fragments of nagative and positive images.
By selection of images made by humans. Question still remains 'How much we can trust humans' decision of selection'.
### Inaccurate cascade classifiers trained.
Small amount of inaccurate images would make significant imprecise cascade classifiers.