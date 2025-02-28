# HearthStone

## What..
HearthStone adds a consumable stone that brings you back home.

- You can set any bed of yours to be the Hearthstone "spawn" point
- You can craft Hearthstone in your Backpack with configurable items
- Now the HearthStone is a heart stone
- May be attached to item stands

The Mod must be installed in the server and the client.

## Config

### Section [General]
setting | value | meaning
--------|-------|--------
RecipeItem1 | string | The ItemName (PrefabName) of the 1st item needed to craft the HS
RecipeItem2 | string | The ItemName (PrefabName) of the 2nd item needed to craft the HS
RecipeItem3 | string | The ItemName (PrefabName) of the 3rd item needed to craft the HS
itemCost1 | int | The amount of the 1st item needed to craft the HS
itemCost2 | int | The amount of the 2nd item needed to craft the HS
itemCost3 | int | The amount of the 3rd item needed to craft the HS
allowTeleportWithoutRestriction | true / false | when set to false HS will respect the teleport limitations iE metal in inventory

#### Hint: 
If you only want to use one or two items to craft a HearthStone you may leave either the name of RecipeItem2 (or/and RecipeItem3) blank or alternatively set the itemCost2 (and/or itemCost3) to 0.


### Section [Debug]
setting | value | meaning
--------|-------|--------
writeDebugOutput | true / false | Indicates whether additional mod output will be written to the console

## Versions
### 1.0.7
+ updated Version Reference for Jotunn to be 2.23.2
+ 
### 1.0.6
+ updated Version Reference for Jotunn to be 2.10.4

### 1.0.5
+ moved from using ingame Stone to Assetbundle with own mesh
+ implemented new texture
+ some code improvements
+ stones may be attached to horizontal and vertical item stand
+ leaving recipe item empty should no longer produce an error on the console
+ removed falsely assigned "pickable" script from asset which prevented picking up a dropped Hearthstone

### 1.0.4
+ changed base item to be Stone, not Yagluth Thing anymore
+ added correct overlay texture
+ removed old image files

### 1.0.3
+ hotfix for missing HS icon

### 1.0.2 
+ changed the recipe to be able to be limited to 1 or 2 materials 
+ now the config is synchronized between client/server
+ updated ingame icon

### 1.0.1
Working Version

### 1.0.0
Initial Version

## Thanks..
.. go to [Detalhes](https://valheim.thunderstore.io/package/Detalhes/) for the [original mod](https://valheim.thunderstore.io/package/Detalhes/Hearthstone/)
.. to the great, supporting [community of the J&ouml;tunn Mod](https://discord.gg/DdUt6g7gyA) - you are awesome!
.. and to beloved "<b>schattentraum</b>" for her wish to have a current working version