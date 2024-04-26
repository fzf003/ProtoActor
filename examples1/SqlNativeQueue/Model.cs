using System.ComponentModel.DataAnnotations;

public enum M2RuleReferNode
    {
        分期优惠,
        首期款,
        二期款,
        中期款,
        尾期款
    }

     public static class EnumTypeExtention
    {
        public static T GetDisplay<T>(this string name)
        {
            foreach (var memberInfo in typeof(T).GetMembers())
            {
                foreach (var attr in memberInfo.GetCustomAttributes(true))
                {
                    var test = attr as DisplayAttribute;

                    if (test == null) continue;

                    if (test.Name == name)
                    {
                        var result = (T)System.Enum.Parse(typeof(T), memberInfo.Name);

                        return result;
                    }
                }
            }

            return default(T);
        }

        public static string GetDisplay<T>(this T type, System.Enum enm) where T : Type
        {
            foreach (var memberInfo in type.GetMembers())
            {
                foreach (var attr in memberInfo.GetCustomAttributes(true))
                {
                    var test = attr as DisplayAttribute;

                    if (test == null) continue;

                    if (memberInfo.Name == enm.ToString())
                    {
                        return test.Name;
                    }
                }
            }

            return null;
        }

        public static T GetEnum<T>(this string enumItem) where T : System.Enum
        {
            if(!System.Enum.IsDefined(typeof(T),enumItem))
            {
                return default;
            }
            var val = (T)System.Enum.Parse(typeof(T), enumItem);
            return val;
        }
    }