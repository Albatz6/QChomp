# QChomp
This is a small project which shows how machine learning might be used in a simple [game of Chomp](https://en.wikipedia.org/wiki/Chomp).
AI uses reinforcement learning technique called Q-learning and therefore is able to learn how to play Chomp playing against itself.

The project currently consists of the core library, console application and basic web app created with Razor Pages to showcase the AI on a 6x9 field.
Console application can be used for testing various field sizes as well as AI settings (epsilon rate, learning rate and number of training games).
Generated AI models might be saved and loaded as JSON-files. Every model saving creates two files: AI q-table and training statistics.
Training statistics represents a list of objects, each of which contains info about training iteration, number of new positions found and epsilon rate
used at the given iteration. Thus, it's possible to plot a line chart to analyze training settings.

## How to use
Download the latest verison from Releases page and run.
```
Usage: consoleqchomp.exe [OPTION...]
 -e|--eps           Set epsilon rate (0.1 by default).
 -lr|--lrate        Set lerning rate (0.5 by default).
 -ng|--nogame       Run in training mode without gameplay.
 --save             Save model & training stats with autogenerated name.
 --save [filename]  Save model & training stats with the given name.
 --load [filename]  Load model from file.
```