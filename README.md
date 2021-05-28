# 2D Cloth

The 2D cloth system for Unity. Use _"2D Object -> 2D Cloth"_ to create new cloth sprite gameobject and _"2D Wind"_ for new wind gameobject.
This package contains 2D Cloth and 2D Wind to support multiple clothes with wind system.

## Cloth2D
![image](https://user-images.githubusercontent.com/12474900/118684370-1e8cec80-b83d-11eb-8826-75f085867fde.png)
<br>
Support multiple clothes. Consider which resolution is the best option for your game performance.
The collision only supports kinematic or dynamic rigidbody by default.

> Add _"ClothKinematicReceiver"_ to a gameobject if the gameobject has a kinematic rigidbody.
> Otherwise, the cloth can't know whether the kinematic object has moved or not.

<br>
If you want to allow a cloth detects [another cloth, static rigidbody, pure collider], Add dynamic rigidbody manually to the cloth.
In this case, make sure if you locked positionXY and rotationZ and the gravity scale as 0.

> ### NOTE)
> If you rotate or scale 2d cloth, note that they will apply original values to the cloth and automatically update the rotation & scale in the editor to default at runtime(play mode).
> <br>
> The original values(rotation, scale) in the editor which you modified will be restored once you quit play mode.
> #### TL;DR:
> It just looks like the rotation and scale are changed in play mode, but it does not affect at all in practice. :)


## Wind2D
![image](https://user-images.githubusercontent.com/12474900/115981638-2061e800-a5d0-11eb-9c69-5a6bcf7d6ad5.png)
<br>
Support multiple winds. You can control 2D clothes with this component.
