using System;
using System.Collections.Generic;
using System.Text;

namespace MESHConverter
{
  public class Vertex
  {
    public float X
    {
      get; set;
    }

    public float Y
    {
      get; set;
    }

    public float Z
    {
      get; set;
    }

    public float XNormal
    {
      get; set;
    }

    public float YNormal
    {
      get; set;
    }

    public float ZNormal
    {
      get; set;
    }

    public float U
    {
      get; set;
    }

    public float V
    {
      get; set;
    }

    public ushort D1
    {
      get; set;
    }

    public ushort D2
    {
      get; set;
    }

    public bool PossibleNormal
    {
      get
      {
        return NormalCheck >= 0.9 && NormalCheck <= 1.1;
      }
    }

    public double NormalCheck => Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2);
  }
}
