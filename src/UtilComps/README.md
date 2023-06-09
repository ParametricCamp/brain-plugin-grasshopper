# Utility components

These are components that aren't specific to any single service and can be potentially shared.

## Convert to Bitmap
Takes URL or Base64 encoding of an image and creates `System.Drawing.Bitmap` object.

## Enviroment Variable
Reads locally defined user/system enviroment variables

## HTTP Get Request Async
A generic component to do GET requests asynchronously, to prevent locking of GH UI

## HTTP Get Request
A generic component to do GET requests synchronously, locks GH UI while waiting for response

## HTTP Post Request Async
A generic component to do POST requests asynchronously, to prevent locking of GH UI

## HTTP Post Request
A generic component to do POST requests synchronously, locks GH UI while waiting for response

## Image Viewer
Previews `System.Drawing.Bitmap` objects

## Save Bitmap Locally
Takes `System.Drawing.Bitmap` objects and saves them to the specified folder. If `Bitmap` object is created from a url, file name will be the same as the filename in the url, else the file name will be the "date and time" of creation.