# 2D Cloth
![image](https://user-images.githubusercontent.com/12474900/120170605-191ba300-c23c-11eb-8993-33a640d6bdb1.png)

The 2D cloth system for Unity. Use _"2D Object -> 2D Cloth"_ to create new cloth2D gameobject and _"2D Wind"_ for new wind gameobject.
This package contains 2D Cloth and 2D Wind to support multiple clothes with wind system.

See WebGL Demo here: [Link](http://3.34.238.156/cloth2d "Demo link")

## Cloth2D
![image](https://user-images.githubusercontent.com/12474900/120444557-b1439480-c3c2-11eb-8ac0-7bb1a11bb891.png)
<br>
Support multiple clothes. Consider which resolution is the best option for your game performance.
The collision only supports kinematic or dynamic rigidbody by default.

> Add _"Cloth2DKinematicReceiver"_ to a gameobject if the gameobject has a kinematic rigidbody.
> Otherwise, the cloth can't know whether the kinematic object has moved or not.

<br>
If you want to allow a cloth detects [another cloth, static rigidbody, pure collider(not trigger)], Add dynamic rigidbody manually to the cloth.
In this case, make sure if you locked positionXY and rotationZ and the gravity scale as 0.
<br>

> ### NOTE)
> If you rotate or scale 2d cloth, note that they will apply original values to the cloth and automatically update the rotation & scale in the editor to default at runtime(play mode).
> <br>
> The original values(rotation, scale) in the editor which you modified will be restored once you quit play mode.
> #### TL;DR:
> It just looks like the rotation and scale are changed in play mode, but it does not affect at all in practice. :)


## Wind2D
![image](https://user-images.githubusercontent.com/12474900/115981638-2061e800-a5d0-11eb-9c69-5a6bcf7d6ad5.png)
<br>
Support multiple winds.
Note that the wind affects a cloth if the cloth's world transform position is in its boundary.


## Cloth2DChain
![image](https://user-images.githubusercontent.com/12474900/120443996-2a8eb780-c3c2-11eb-9424-6ee5ea47dbf7.png)
<br>
Simple version of cloth2D, which is specialized for 2d chain stuff. It also supports multiple chain clothes.
Note that it doesn't support rotation and collision.
