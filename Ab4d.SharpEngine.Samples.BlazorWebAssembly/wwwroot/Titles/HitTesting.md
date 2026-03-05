# Hit testing

Hit testing allows user interaction with the shown 3D objects.

Hit testing is usually done by calling **GetClosestHitObject** or **GetAllHitObjects** method on SceneView object. Behind the scene this creates a 3D ray from the camera and the code checks which triangles from the 3D scene are hit by the ray.

This method cannot be used to hit test 3D lines. There a **LineSelectorData** class is used that can also return the lines that are close to the mouse.