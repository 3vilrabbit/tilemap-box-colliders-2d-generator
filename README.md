# Tilemap Box Colliders 2D Generator
This scripts automatically generate a certain number of BoxCollider2D to cover a Tilemap.
This can improve physics computations. Currently only BoxCollider2D are used, tiles with non-square physics shape are not supported (I may add support in future)
The idea came after I watched this video: https://www.youtube.com/watch?v=DUB5iiwULaI&ab_channel=RogerSchoellgen

## How to use it
- Add the TilemapCollidersGenerator script to your desired TileMap grid.
- Choose if you want the colliders to be generated in the same GameObject as the Tilemap or in a child GameObject.
- Choose if you want to allow the colliders to overlap. Overlapping colliders will result in bigger colliders but it can reduce the number of overall colliders in certain situations.
- Press the "Generate colliders" button to run the script. All previously generated Colliders will be removed before generating the new ones.

NOTE: You can use the "Remove all colliders" button to remove them all.


![image](https://user-images.githubusercontent.com/15826298/234534866-e76a43a6-d522-4427-9527-101de6fe3aac.png)

