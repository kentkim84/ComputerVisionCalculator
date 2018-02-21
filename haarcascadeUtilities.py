# reference: https://pythonprogramming.net/haar-cascade-object-detection-python-opencv-tutorial/
# *python3 code*
import urllib.request
import cv2
import numpy as np
import os

# get the images sources from the urls
# modify and store the images
def store_raw_images():
    #negative image source url 1: http://image-net.org/api/text/imagenet.synset.geturls?wnid=n01316949
    #negative image source url 2: http://image-net.org/api/text/imagenet.synset.geturls?wnid=n00021265
    #negative image source url 3: http://image-net.org/api/text/imagenet.synset.geturls?wnid=n10529231    
    neg_images_link = 'http://image-net.org/api/text/imagenet.synset.geturls?wnid=n10529231'
    neg_image_urls = urllib.request.urlopen(neg_images_link).read().decode()
    pic_num = 1
    
    # if the 'neg' folder doesn't exist, it will create a folder
    if not os.path.exists('neg'):
        os.makedirs('neg')
        
    for i in neg_image_urls.split('\n'):
        try:
            print(i)
            urllib.request.urlretrieve(i, "neg/"+str(pic_num)+".jpg")
            img = cv2.imread("neg/"+str(pic_num)+".jpg", cv2.IMREAD_GRAYSCALE)
            # should be larger than samples / pos pic (so we can place our image on it)
            resized_image = cv2.resize(img, (100, 100))
            cv2.imwrite("neg/"+str(pic_num)+".jpg",resized_image)
            pic_num += 1
            
        except Exception as e:
            print(str(e))

# images that downloaded from url that no-longer exist anymore, by default system downloaded images that not needed
# find and remove unnecessary images from the 'neg' folder
def find_uglies():
    for file_type in ['neg']:
        for img in os.listdir(file_type):
            for ugly in os.listdir('uglies'):
                try:
                    current_image_path = str(file_type)+'/'+str(img)
                    ugly = cv2.imread('uglies/'+str(ugly))
                    question = cv2.imread(current_image_path)                    
                    # 1. if both ugly and question have the same size
                    # 2. if both ugly and question have the same image
                    if ugly.shape == question.shape and not(np.bitwise_xor(ugly,question).any()):                        
                        print(current_image_path)
                        os.remove(current_image_path)
                except Exception as e:
                    print(str(e))
                    

def create_pos_n_neg():
    for file_type in ['neg']:                
        for img in os.listdir(file_type):
            # only modify positive images
            # for training and getting accurate cascade purpose
            # not be needed now
            if file_type == 'pos':
                line = file_type+'/'+img+' 1 0 0 50 50\n'
                with open('info.dat','a') as f:
                    f.write(line)
            # create a background info text file
            # be used to create samples
            elif file_type == 'neg':
                line = file_type+'/'+img+'\n'
                with open('bg.txt','a') as f:
                    f.write(line)

#store_raw_images()
create_pos_n_neg()
#find_uglies()
