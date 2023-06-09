# OpenAI components

Implemented based on the [OpenAI API documentation](https://platform.openai.com/docs/api-reference)

Componets outputs will generally have 2 outputs, the raw response and a filtered/main data; some endpoints may return different types of data depending on the request parameters, if this is the case, there may be multiple filtered/main outputs

Some endpoints returns data that cannot be used directly, such as a Base64 encoded image, these data would need components from the Utilities category to convert them to more usable and grasshopper-friendly object.

## Advanced options
![](../../doc/OpenAI/advancedOptions.webp)
By default, components exposes the minimum amount of inputs for a clean look, if you wish to expose additional options, you can enable them in the context menu by right clicking on the component. Do note that some components have **MANY** advanced options.

Note: not all components have advanced options

## Categorzations
I personally think it'd be terrible if a single plugin created a whole bunch of tabs/categories in grasshopper, so all components are categorized under the Brain category/tab, in which there's the sub-category for OpenAI.
In grasshopper, each sub-category can have a maximum of 7 sections, so I've sectioned the components based on their functionality, and the sections are as follows:
1. Text
2. Image
3. Audio
4. Files
5. Fine-tune
6. Misc (embeddings and moderations)

## Progress overview
![](../../doc/OpenAI/overview.webp)

Status |Component | Section | Assigned to | Progress | Icon | Doc | Remarks
:---:|---|:---:|:---:|---|:---:|:---:|---
:heavy_check_mark: | List models | senary | [@garciadelcastillo](https://github.com/garciadelcastillo)<br>[@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:
| | Retrieve model | | | | | | Seems pretty useless to implement? -ycv
:heavy_check_mark: |Create completion | primary | [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:| `stream` and `logit_bias` not implemented due to complexity -ycv
:heavy_check_mark: | Create chat completion | primary | [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|  `stream` and `logit_bias` not implemented due to complexity; `name` also not implemented, as it's more metadata than functional -ycv
:heavy_check_mark: | Create edit | primary |[@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:| 
:heavy_check_mark: | Create image | secondary |[@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:| 
:heavy_check_mark: | Create image edit | secondary¡@| [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|
:heavy_check_mark: | Create image variation | secondary¡@| [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|
:heavy_check_mark: | Create embeddings | senary¡@| [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|
:heavy_check_mark: | Create transcription¡@| tertiary¡@| [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|
:heavy_check_mark: | Create translation | tertiary¡@| [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|
:heavy_check_mark: | List files |quarternary¡@| [@lin-ycv](https://github.com/lin-ycv) | Finished | :x:| :x:|
:heavy_check_mark: | Upload file¡@| quarternary¡@| [@lin-ycv](https://github.com/lin-ycv) | Test pending | :x:| :x:| Did not test, need properly formated JSONL file -ycv
:heavy_check_mark: | Delete file | quarternary¡@| [@lin-ycv](https://github.com/lin-ycv) | Test pending | :x:| :x:| Did not test, no files uploaded -ycv
:heavy_check_mark: | Retrieve file | quarternary¡@| [@lin-ycv](https://github.com/lin-ycv) | Test pending | :x:| :x:| Did not test, no files uploaded -ycv
| | Retrieve file content | quarternary
| | Create fine-tune | quinary
| | List fine-tunes | quinary
| | Retrieve fine-tune | quinary
| | Cancel fine-tune | quinary
| | List fine-tune events | quinary
| | Delete fine-tune model | quinary
| | Create moderation |senary

:heavy_check_mark: Finished and working (but may be missing some features, check remarks)<br>
:heavy_minus_sign: Working but have some issues or may need refinement<br>
:x: Should have but missing<br>
:heavy_exclamation_mark: Problematic and not implemented