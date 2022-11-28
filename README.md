# Hearthstone

## What..
Hearthstone adds a consumable stone that brings you back home.

- You can set any bed of yours to be the Hearthstone stone "spawn" point
- You can craft hearthstone in workbench with configurable items
- Now the hearthstone is a heart

It must be installed in the server and the client.

## Config

### Section [General]
setting | value | meaning
--------|-------|--------
RecipeItem1 | string | The ItemName of the 1st item needed to craft the HS
RecipeItem2 | string | The ItemName of the 2nd item needed to craft the HS
RecipeItem3 | string | The ItemName of the 3rd item needed to craft the HS
itemCost1 | int | The amount of the 1st item needed to craft the HS
itemCost2 | int | The amount of the 2nd item needed to craft the HS
itemCost3 | int | The amount of the 2rd item needed to craft the HS
allowTeleportWithoutRestriction | true / false | when set to false HS will respect the teleport limitations iE metal in inventory

#### Hint: 
If you only want to use one or two items to craft a HearthStone you may leave either the name of RecipeItem2 (or/and RecipeItem3) blank or alternatively set the itemCost2 (and/or itemCost3) to 0.


### Section [Debug]
setting | value | meaning
--------|-------|--------
writeDebugOutput | true / false | Indicates whether additional mod output will be written to the console


## Versions
### 1.0.2 
+ changed the recipe to be able to be limited to 1 or 2 materials 
+ now the config is synchronized between client/server

### 1.0.1
Working Version

### 1.0.0
Initial Version

## Thanks..
.. go to [Detalhes](https://valheim.thunderstore.io/package/Detalhes/) for the [original mod](https://valheim.thunderstore.io/package/Detalhes/Hearthstone/)

.. and to beloved "<b>schattentraum</b>" for her wish to have a current working version