```csharp
// ██████╗ ██████╗  █████╗ ██╗███╗   ██╗
// ██╔══██╗██╔══██╗██╔══██╗██║████╗  ██║
// ██████╔╝██████╔╝███████║██║██╔██╗ ██║
// ██╔══██╗██╔══██╗██╔══██║██║██║╚██╗██║
// ██████╔╝██║  ██║██║  ██║██║██║ ╚████║
// ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝
//                                      
// ██████╗ ██╗     ██╗   ██╗ ██████╗ ██╗███╗   ██╗
// ██╔══██╗██║     ██║   ██║██╔════╝ ██║████╗  ██║
// ██████╔╝██║     ██║   ██║██║  ███╗██║██╔██╗ ██║
// ██╔═══╝ ██║     ██║   ██║██║   ██║██║██║╚██╗██║
// ██║     ███████╗╚██████╔╝╚██████╔╝██║██║ ╚████║
// ╚═╝     ╚══════╝ ╚═════╝  ╚═════╝ ╚═╝╚═╝  ╚═══╝
//                                                
```

# BRAIN PLUGIN
This is a document for planning the new AI plugin for GH made by the ParametricCamp community! 🏕

## KICK-OFF
- Background research: what's available today
- Brainstomring of components/categories
- Planning a draft of the structure of the plugin

## PHILOSOPHY/GOALS
- Not ML from scratch: mostly components to make it easier to **access existing APIs**. 
- We want **Open Source**
- We want to **avoid dependencies/processes running in the BG**
- Every component/API endpoint/process should come with a **sample file**
- This plugin is about **democratizing access to ML** and helping people explore possibilities creatively. This is not about teaching the underlying principles of ML (regression, NN architectures, etc.).
- We want to favor free APIs, but we are ok with paid services (OpenAI), as long as **it's not us (PCamp)** paying for them! :)

## BACKGROUND RESEARCH
Current projects out there:
- **PUG for GH**: implementation of Tensorflow.NET --> We want mostly access to APIs, not ground-up ML.  
- **Dodo**: assorted collection of ML tools, not OS. 
- **Owl**: similar to PUG?
- **Lunchbox**: some ML components. 
- **Ambrosinus Toolkit**: looks very similar to what we want to do! Very nice and comprehensive! ~~Not OS?~~ https://github.com/lucianoambrosini/Ambrosinus-Toolkit --> only examples? 
- **Catwalk**: pose estimation only.
- **RunwayML**: only runway integration... Maybe we want to implement bindings to the RunwayML API? 



## APIS THAT WE COULD IMPLEMENT

### OpenAI
  - Text Completion
  - Chat
  - Image Generation
  - Fine Tuning? --> Too much? 
  - Speech to Text --> interesting, how do we implement audio in GH? 
  - Image Generation --> Image tools for visualization, editing... Firefly has a bunch of those, not OS. 

### IBM
  - Watson Assistant: https://cloud.ibm.com/catalog/services/watson-assistant
  - Language Translator: https://cloud.ibm.com/catalog/services/language-translator
  - Natural Language Understanding: https://cloud.ibm.com/catalog/services/natural-language-understanding?catalog_query=aHR0cHM6Ly9jbG91ZC5pYm0uY29tL2NhdGFsb2c%2FY2F0ZWdvcnk9YWk%3D
  - Text <> Speech: https://cloud.ibm.com/catalog/services/text-to-speech?catalog_query=aHR0cHM6Ly9jbG91ZC5pYm0uY29tL2NhdGFsb2c%2FY2F0ZWdvcnk9YWk%3D
  
### HuggingFace
  - Stable Diffusion
  - Generic Access to any HF model? 

### RunwayML
Looks like hosted models are in the process of deprecation? Maybe we could still explore:
- API calls to hosted models in ML Lab
- API calls to web-based hosted models.

Any of this would have low priority... 

### HYPAR
Looks like they don't host ML models, BUT, maybe they host processes? Can we access them through a REST API? Looks like you need to install the CLI tool, which is kind of a downer... 😟

Maybe take a look, but low priority too? 

Maybe reach out to Ian and/or Andrew? 

### AZURE
Looks like they have an IBM-like set of ML models? 
https://azure.microsoft.com/en-us/products/cognitive-services/#api
Are they free/accessible? We should give these a try... 
They have OpenAI integration

### GOOGLE 
Looks like Google has some AI for developers APIs:
https://cloud.google.com/products#section-3
Maybe take a look at these...? 



## CATEGORIZATION
Break them down by platform?
- OpenAI
- IBM
- Google
- Azure
- HuggingFace
This sounds about right for now, but perhaps requires knowledge from the user about what can be done in which platform? 

By topic?
- Text
- Image
- Audio
This could be difficult to categorize or come up with rules for what is where. Too generic. 

In any case, we should implement some **generics**
- **Generic requests**: **GET**, **POST**, etc. 
- **Data processing**: show image, encode image in base64, audio processing/encoding, image processing (masks), point cloud from image + depth? save image to system? JSON parsing (ship jSwan with the plugin, talk to Andrew)? Get environment variable, image format conversion (e.g. JPEG to PNG)

## OTHER DESIGN CONSIDERATIONS
- Every component that does an API request should be **Async (Speckle template)** --> show elapsed time/completion in the message box
- Maybe we even extend the template with a custom **subclass that handles the request**, the updating of the UI and receiving the output, and most request components inherit this class and only need to implement IOs + request composition + response deserialization. 
- Every request component should always at least output the **raw response**.
- If a response has **array data**, these should be properly converted to **GH lists/trees**.


## DRAFT OF AN MVP
Each heading is going to be a GH Category. Each bulletpoint, a component.

In parenthesis, let's add a priority level:
- 5 is *critical for MVP*
- 4 is *interesting but not critical*
- 3 is *nice to have, no rush*
- 2 is *low priority*
- 1 is *it would be nice at some point*

### OPENAI
- List Models (5)
  - Needs API Key, returns models
- Text Completion (5)
  - Follow the API IOs

- Chat (4)
  - How is this different from completion? How could we implement this idea of continuous conversation with a chat component? We need to think/design this, requires research
- Edits (4)
  - Not sure how this works, needs research. 
- Create Image (5)
  - Follow the API IOs
  - Should we output url/base64 only, and require users to convert (with out data tools)? Or should we also directly convert to a Bitmap that can be rendered? For base64 it is trivial, but for url responses, we would need to do an additional GET request to fetch the image data from the url... not sure about one way or the other... 
- Create image edit (5)
  - Follow the API IOs
  - Interesting application of taking an image as input and working with it. 
  - Needs PNG files, so also interesting to do file type checks.
  - Also interesting to work with masks and transparency
- Create image variation (5)
  - Similar to the above (perhaps easier)
- Embeddings (3)
  - Low priority, unless it can easily be used as inputs for other processes
  - It sounds trivial to implement, but I wouldn't do it unless we can come up with a good example of why this is useful.
- Create transcription (4)
- Create translation (4)
  - These would require working with audio data: reading audio from a file, or recording audio with a GH component + button. 
- Files (4)
  - There is `List files`, `Upload` `delete` `retrieve`, not sure what these are used for, but we could develop these as a package. 
  - Find a good example of why do we want to store files in OpenAI
- Moderation (5)
  - Easy one, get results for content moderation


## HOW DO WE BREAK THIS DOWN INTO VIDEOS FOR A PLAYLIST?
- [x] General intro (do a temp one and then replace with the final thing)
- [x] Overview of the goals (read this document)
- [x] Starting the repo and the VS project
- [ ] Raw POST component
  - [x] Prototype it real quick
  - [ ] First do a blocking version
  - [ ] Then upgrade to async version
  - [ ] Modify speckle to accept `string` updates vs. `double` updates
- [ ] Raw GET component --> extension of the above.
- [ ] Simple OpenAI Text Completion component (Async)
- [ ] Some other component
- [ ] Create a superclass for components with requests? (take the two prev components and abstract their common func)
- [ ] More components? 

### OTHER VIDEOS
- Accepting someone's PR

## CHATGPT-SUGGESTED PLUGIN NAMES
1. MLink for Grasshopper
2. MachineLearnerGH
3. MLConnect3D
4. RhinoAI
5. AIModelerGH
6. GrassML
7. MLHopper
8. SmartRhino
9. AIAssistedDesign
10. RhinoNeuralNet

Lame! We are sticking to `Brain` for the time being 🧠


# TAGS FOR VIDEOS
AI, Artificial Intelligence, ML, Machine Learning, Grasshopper,Scripting,Development,C#,Parametric,Modeling,Computational,Geometry,Design,Algorithm,Algorithmic,Rhino,Rhinoceros,Designers,Vector,Algebra,Introduction,3D,2D,CAD,BIM