﻿// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Hamster {
  // Monobehaviour for the actual unity gameobject that represents the map.
  // Largely concerned with spawning the scene representation of a level
  // and tearing it down when done.
  class GameWorld : MonoBehaviour {
    // Dictioary of scene object representations of the map tiles and elements.
    // The key is the same key as they have in the database table.  (A unique
    // string based on their position and type.)
    Dictionary<string, GameObject> sceneObjects = new Dictionary<string, GameObject>();
    public LevelMap worldMap = new LevelMap();

    // When spawning in elements, adjust their positions by this amount.
    Vector3 elementAdjust = new Vector3(0.0f, -0.5f, 0.0f);

    public float GameStartTime { get; private set; }
    public float ElapsedGameTime {
      get {
        return Time.time - GameStartTime;
      }
    }

    private void Start() {
      GameStartTime = Time.time;
    }

    // Iterates through a map and spawns all the objects in it.
    public void SpawnWorld(LevelMap map) {
      foreach (MapElement element in map.elements.Values) {
        GameObject obj = PlaceTile(element);
        obj.transform.localScale = element.scale;
      }
      worldMap.SetProperties(map.name, map.mapId, map.ownerId);
    }

    public void DisposeWorld() {
      worldMap.elements.Clear();
      foreach (GameObject obj in sceneObjects.Values) {
        Destroy(obj);
      }
      sceneObjects.Clear();
      worldMap.ResetProperties();
    }

    // Internal utility function for removing an item from the map, based on its
    // dictionary key.  (The dictionary key is the string returned from
    // MapElement::GetKey())
    void RemoveObject(string key) {
      worldMap.elements.Remove(key);
      Destroy(sceneObjects[key]);
      sceneObjects.Remove(key);
    }

    // Spawns a single object in the world, based on a map element.
    public GameObject PlaceTile(MapElement element) {
      string key = element.GetStringKey();

      // If we're placing a unique object, or we're placing an object over an existing object,
      // then remove the old one that is being replaced.
      if (worldMap.elements.ContainsKey(key)) {
        RemoveObject(key);
      }

      // Add the new object.  (Or add nothing, if we're "adding" an empty blocks)

      GameObject obj = SpawnElement(element);
      if (obj != null) {
        worldMap.elements.Add(key, element);
        sceneObjects.Add(element.GetStringKey(), obj);
      }
      return obj;
    }

    GameObject SpawnElement(MapElement element) {
      GameObject obj = null;
      PrefabList.PrefabEntry elementDef;
      if (CommonData.prefabs.lookup.TryGetValue(element.type, out elementDef)) {
        if (elementDef.prefab != null) {
          Quaternion orientation = Quaternion.Euler(
              new Vector3(0.0f, element.orientation * 90.0f, 0.0f));
          obj = (GameObject)Instantiate(elementDef.prefab, element.position + elementAdjust,
                                        orientation);
        }
      } else {
        throw new Exception(
            "SpawnElement: type did not match any registered prefabs: [" + element.type + "]");
      }
      return obj;
    }

    // Reset the Map back to its original state. This includes the game time since it started,
    // and the MapObjects
    public void ResetMap() {
      GameStartTime = Time.time;

      foreach (GameObject gameObject in sceneObjects.Values) {
        MapObjects.MapObject mapObject = gameObject.GetComponent<MapObjects.MapObject>();
        if (mapObject != null) {
          mapObject.Reset();
        }
      }
    }
  }
}
