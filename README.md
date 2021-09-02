# CawkVM Unpacker
A tool designed to restore code protected by CawkVM.

![CawkVm Unpacker](https://i.imgur.com/OHTFYaA.jpeg)

### Why?
Pretty much every other tool I've seen relies on reflection and uses the original runtime component to construct a DynamicMethod which is then converted to raw CIL. Since invoking methods via reflection may lead to unwanted code executing when misused I decided to create a tool which doesn't use reflection at all. Moreover, I've been hearing a lot that the exisitng versions have bugs/don't work very well so I hope this project will :)

### Usage
Pass a file path as an argument and let the magic happen.

###### Help, I got an error!
You can use the `-d` argument to specify the name of resource containing method data so the tool doesn't have to go hunting in the forest for it.

### License
[GPL-v3.0](https://github.com/ElektroKill/CawkVM-Unpacker/blob/master/LICENSE)
