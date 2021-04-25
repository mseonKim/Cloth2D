# 2D Cloth

The 2D cloth system for Unity. Use _"2D Object -> 2D Cloth"_ to create new cloth sprite gameobject and _"2D Wind"_ for new wind gameobject.
This package contains 2D Cloth and 2D Wind to support multiple clothes with wind system.

## Cloth2D
![image](https://user-images.githubusercontent.com/12474900/115981297-9ca6fc00-a5cd-11eb-9356-57743bc798c7.png)
<br>
Support multiple clothes. Consider which resolution is the best option for your game performance.

### NOTE
If you rotate or scale 2d cloth, note that they will apply original values to the cloth and automatically update the rotation & scale in the editor to default at runtime(play mode).
<br>
The original values(rotation, scale) in the editor which you modified will be restored once you quit play mode.
#### TL;DR:
It just looks like the rotation and scale are changed in play mode, but it does not affect at all in real :)


## Wind2D
![image](https://user-images.githubusercontent.com/12474900/115981638-2061e800-a5d0-11eb-9c69-5a6bcf7d6ad5.png)
<br>
You can control 2D clothes with this component. Support multiple wind gameobjects in the same scene.
