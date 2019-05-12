using Google.Cloud.Vision.V1;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GcpVisionLineSegmentation
{
  public class GoogleVisionLineSegmentation
  {
    public static List<string> GetLineSegmentation(IReadOnlyList<EntityAnnotation> data)
    {
      var mergedArray = Initialization(data);

      CoordinatesHelper.GetBoundingPolygon(mergedArray);
      CoordinatesHelper.CombineBoundingPolygon(mergedArray);

      // This does the line segmentation based on the bounding boxes
      return ConstructLineWithBoundingPolygon(mergedArray);
    }

    static List<ReceiptEntityAnnotation> Initialization(IReadOnlyList<EntityAnnotation> data)
    {
      // Computes the maximum y coordinate from the identified text blob
      int yMax = data[0].BoundingPoly.Vertices.Max(y => y.Y);
      CoordinatesHelper.InvertAxis(data, yMax);

      // The first index refers to the auto identified words which belongs to a sings line
      List<string> lines = data[0].Description.Split('\n').Reverse().ToList();

      // gcp vision full text and remove the zeroth element which gives the total summary of the text
      // reverse to use lifo, because array.shift() will consume 0(n)
      var rawText = data.Skip(1).Reverse().ToList();

      return GetMergedLines(lines, rawText);
    }

    // TODO implement the line ordering for multiple words
    static List<string> ConstructLineWithBoundingPolygon(List<ReceiptEntityAnnotation> mergedArray)
    {
      List<string> finalArray = new List<string>();

      for (int i = 0; i < mergedArray.Count; i++)
      {
        if (!mergedArray[i].Matched)
        {
          if (mergedArray[i].Match.Count == 0)
          {
            finalArray.Add(mergedArray[i].Description);
          }
          else
          {
            // arrangeWordsInOrder(mergedArray, i);
            // let index = mergedArray[i]['match'][0]['matchLineNum'];
            // let secondPart = mergedArray[index].description;
            // finalArray.push(mergedArray[i].description + ' ' +secondPart);
            finalArray.Add(ArrangeWordsInOrder(mergedArray, i));
          }
        }
      }
      return finalArray;
    }

    static string ArrangeWordsInOrder(List<ReceiptEntityAnnotation> mergedArray, int k)
    {
      StringBuilder mergedLine = new StringBuilder();
      // let wordArray = [];
      var line = mergedArray[k].Match;
      // [0]['matchLineNum']
      for (int i = 0; i < line.Count; i++)
      {
        int index = line[i].MatchLineNum;
        string matchedWordForLine = mergedArray[index].Description;

        int mainX = mergedArray[k].Vertices[0].X;
        int compareX = mergedArray[index].Vertices[0].X;

        if (compareX > mainX)
        {
          mergedLine.Append(mergedArray[k].Description + ' ' + matchedWordForLine);
        }
        else
        {
          mergedLine.Append(matchedWordForLine + ' ' + mergedArray[k].Description);
        }
      }
      return mergedLine.ToString();
    }

    static List<ReceiptEntityAnnotation> GetMergedLines(List<string> lines, List<EntityAnnotation> rawText)
    {
      List<ReceiptEntityAnnotation> mergedArray = new List<ReceiptEntityAnnotation>();
      while (lines.Count != 1)
      {
        string
          l = lines.Pop(),
          l1 = l,
          data = string.Empty;

        ReceiptEntityAnnotation mergedElement = null;

        while (rawText.Count > 0)
        {
          EntityAnnotation wElement = rawText.Pop();
          string description = wElement.Description;

          int index = l.IndexOf(description);
          // check if the word is inside
          l = l.Substring(index + description.Length);
          if (mergedElement == null)
          {
            // set starting coordinates
            mergedElement = new ReceiptEntityAnnotation(/*wElement.BoundingPoly.Vertices*/)
            {
              Description = description,
              Vertices = wElement.BoundingPoly.Vertices
            };
          }
          if (string.IsNullOrEmpty(l))
          {
            // set ending coordinates
            mergedElement.Description = l1;
            mergedElement.Vertices[1] = wElement.BoundingPoly.Vertices[1];
            mergedElement.Vertices[2] = wElement.BoundingPoly.Vertices[2];
            mergedArray.Add(mergedElement);
            break;
          }
        }
      }
      return mergedArray;
    }
  }

  class ReceiptEntityAnnotation
  {
    public ReceiptEntityAnnotation()
    {
      Match = new List<MatchLine>();
    }

    public string Description { get; set; }

    public IList<Vertex> Vertices { get; set; }

    public VertexDouble[] BigBB { get; set; }

    public List<MatchLine> Match { get; set; }

    public bool Matched { get; set; }
  }

  class MatchLine
  {
    public int MatchCount { get; set; }

    public int MatchLineNum { get; set; }
  }

  static class ListExtension
  {
    public static T Pop<T>(this IList<T> list)
    {
      return PopAt(list, list.Count - 1);
    }

    public static T PopAt<T>(this IList<T> list, int index)
    {
      T r = list[index];
      list.RemoveAt(index);
      return r;
    }
  }
}
