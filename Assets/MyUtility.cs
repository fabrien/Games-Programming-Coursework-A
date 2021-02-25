using System;
using UnityEngine;

public static class Utility
{
    public static bool VectorIsInRect(Vector3 goal, Rect rect)
    {
        return goal.x >= rect.xMin && goal.x <= rect.xMax && goal.z >= rect.yMin && goal.z <= rect.yMax;
    }
    
    public static bool VectorIsInRect(Vector2 goal, Rect rect)
    {
        return goal.x >= rect.xMin && goal.x <= rect.xMax && goal.y >= rect.yMin && goal.y <= rect.yMax;
    }

    public static Location Opposite(Location loc)
    {
        return loc switch
        {
            Location.E => Location.W,
            Location.N => Location.S,
            Location.W => Location.E,
            Location.S => Location.N,
            Location.SW => Location.NE,
            Location.SE => Location.NW,
            Location.NW => Location.SE,
            Location.NE => Location.SW,
            _ => throw new ArgumentOutOfRangeException(nameof(loc), loc, null)
        };
    }

    public static bool IsLocationComposed(Location loc)
    {
        return loc switch
        {
            Location.E => false,
            Location.N => false,
            Location.W => false,
            Location.S => false,
            Location.SW => true,
            Location.SE => true,
            Location.NW => true,
            Location.NE => true,
            _ => throw new ArgumentOutOfRangeException(nameof(loc), loc, null)
        };
    }

    public static Location FindCommonLocation(Location loc1, Location loc2)
    {
        if (loc1 == Location.E || loc1 == Location.S || loc1 == Location.N || loc1 == Location.W || 
            loc2 == Location.E || loc2 == Location.S || loc2 == Location.N || loc2 == Location.W ||
            Opposite(loc1) == loc2) {
            throw new Exception("Trying to find a common Location between locations with no common locations");
        }
        if (loc1 == loc2)
            throw new Exception("Same locations.");
        if ((loc1 == Location.NE && loc2 == Location.SE) ||
            (loc2 == Location.NE && loc1 == Location.SE))
            return Location.E;
        if ((loc1 == Location.NE && loc2 == Location.NW) ||
            (loc2 == Location.NE && loc1 == Location.NW))
            return Location.N;
        if ((loc1 == Location.SE && loc2 == Location.SW) ||
            (loc2 == Location.SE && loc1 == Location.SW))
            return Location.S;
        if ((loc1 == Location.NW && loc2 == Location.SW) ||
            (loc2 == Location.NW && loc1 == Location.SW))
            return Location.W;
        throw new Exception("FindCommonLocation of an unhandled case");
    }

    public static Location Substract(Location complex, Location simple)
    {
        return complex switch
        {
            Location.NE when simple == Location.E => Location.N,
            Location.NE when simple == Location.N => Location.E,
            Location.NW when simple == Location.N => Location.W,
            Location.NW when simple == Location.W => Location.N,
            Location.SE when simple == Location.E => Location.S,
            Location.SE when simple == Location.S => Location.E,
            Location.SW when simple == Location.W => Location.S,
            Location.SW when simple == Location.S => Location.W,
            _ => throw new Exception(
                "Either substracting from non complex or substracting complex, or invalid substraction: Complex: " +
                complex + " Simple: " + simple)
        };
    }

    public static String LocationToString(Location loc)
    {
        return loc switch
        {
            Location.E => "E",
            Location.N => "N",
            Location.S => "S",
            Location.W => "W",
            Location.NW => "NW",
            Location.NE => "NE",
            Location.SE => "SE",
            Location.SW => "SW",
            _ => throw new ArgumentOutOfRangeException(nameof(loc), loc, null)
        };
    }

    public static Vector3 Vec2to3(Vector2 v)
    {
        return new Vector3(v.x, 0, v.y);
    }
}