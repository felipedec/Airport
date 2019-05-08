using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Airport {
   public static class TableParser {
      public static string ToStringTable<T>(this IEnumerable<T> Values, string[] ColumnHeaders, params Func<T, object>[] ValueSelectors) {
         return ToStringTable(Values.ToArray(), ColumnHeaders, ValueSelectors);
      }

      public static string ToStringTable<T>(this T[] Values, string[] ColumnHeaders, params Func<T, object>[] ValueSelectors) {
         Debug.Assert(ColumnHeaders.Length == ValueSelectors.Length);

         var ArrValues = new string[Values.Length + 1, ValueSelectors.Length];

         for (int ColIndex = 0; ColIndex < ArrValues.GetLength(1); ColIndex++) {
            ArrValues[0, ColIndex] = ColumnHeaders[ColIndex];
         }
         for (int RowIndex = 1; RowIndex < ArrValues.GetLength(0); RowIndex++) {
            for (int ColIndex = 0; ColIndex < ArrValues.GetLength(1); ColIndex++) {
               object Obj = ValueSelectors[ColIndex].Invoke(Values[RowIndex - 1]);

               ArrValues[RowIndex, ColIndex] = Obj == null ? "<indefinido(a)>" : Obj.ToString();
            }
         }

         return ToStringTable(ArrValues);
      }

      public static string ToStringTable(this string[,] ArrValues) {
         int[] MaxColumnsWidth = GetMaxColumnsWidth(ArrValues);
         var HeaderSpliter = new string('─', MaxColumnsWidth.Sum(I => I + 3) - 1);

         var Sb = new StringBuilder();

         Sb.Append(" ┌").Append(HeaderSpliter).AppendLine("┐");

         for (int RowIndex = 0; RowIndex < ArrValues.GetLength(0); RowIndex++) {
            for (int ColIndex = 0; ColIndex < ArrValues.GetLength(1); ColIndex++) {
               string Cell = ArrValues[RowIndex, ColIndex];
               Cell = Cell.PadRight(MaxColumnsWidth[ColIndex]);
               Sb.Append(" │ ");
               Sb.Append(Cell);
            }

            Sb.Append(" │ ");
            Sb.AppendLine();

            if (RowIndex == 0) {
               Sb.AppendFormat(" ├{0}┤ ", HeaderSpliter);
               Sb.AppendLine();
            }
         }

         Sb.Append(" └").Append(HeaderSpliter).Append("┘");

         return Sb.ToString(); 
      }

      private static int[] GetMaxColumnsWidth(string[,] ArrValues) {
         var MaxColumnsWidth = new int[ArrValues.GetLength(1)];
         for (int ColIndex = 0; ColIndex < ArrValues.GetLength(1); ColIndex++) {
            for (int RowIndex = 0; RowIndex < ArrValues.GetLength(0); RowIndex++) {
               int NewLength = ArrValues[RowIndex, ColIndex].Length;
               int OldLength = MaxColumnsWidth[ColIndex];

               if (NewLength > OldLength) {
                  MaxColumnsWidth[ColIndex] = NewLength;
               }
            }
         }

         return MaxColumnsWidth;
      }
   }
}
 