## Purpose
This document provides an overview of the Final Year Project for Bachelor of Science in Software Development program, including a statement of goals and objectives and a general description of my approach. This working document is directed to my supervisors but should be useful to anyone interested in learning more about creating a Universal Windows Platform(UWP) application with the Microsoft Azure Cognitive Services. This document is something of an executive summary of the project in its current state and summarise the results in phases.

## Goals and Objectives
My primary mission is to comprehend and develop an implementation that gets an input source whether a captured image frame or a loaded image file from the local storage, and distributes the results of Azure Cognitive Service responses.
Furthermore, this implementation can be mpodified in any time for the optimisation purpose.
To achieve this overall objective, I have defined following phases.
* Phase 1: Start creating an app that interacts with the camera device and system IO.
    1. A common sensor on any devices such as mobile, PC, and tablet, that allows to preview the video frame and capture the image frame.
    2. Basic system IO will be used to open and save the file as users request.
* Phase 2: Handle the http request and response.
    1. Computer Vision API - Returns a result of an image analysis that contains information. Tags from the result will be used for displying and keywords to search images.
    2. Image Search API - Returns a result of simmilar images in a Json form depending on which keywords are used.
* Phase 3: Save in the local storage
    1. A single image from an item of the Grid view can be stored in the local storage where the user defiens.
    2. Multiple images from the Grid view can be stored in the pre-defined local storage destinatination as PNG format.

## Challenges and Constraints
### Internet connection is critical
The app is fully workable using Azure Cognitive Services, however, without the internet connection whether 3G/4G or Broadband network connection, this app will not work.
### A system crash may occur
While system is downloading multiple images, users can interact with other functionalities as long as do not attempt to download other images. Download process must be finished before attempting to download the other images.

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
Make sure the internet connection.
Open the project with a suitable IDE (Visual Studio 2017 recommended).
Run the application.
Allow the webcam and microphone permissions.

## Authors
* **Yongjin Kim** - *Initial work* - [Kentkim84](https://github.com/kentkim84)

## References
* Original Project written by [Damien Costello](damien.costello@gmit.ie)
* [Microsoft Azure Computer Vision](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/)
* [Microsoft Azure Bing Image Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-image-search/)
* [Guidelines for list view and grid view](https://msdn.microsoft.com/en-us/library/windows/apps/hh465465.aspx?f=255&MSPPError=-2147217396)