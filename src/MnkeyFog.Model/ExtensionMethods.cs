using System.ComponentModel;
using System.Reflection;

namespace MnkeyFog.Model;

/// <summary>
/// Variant of MaxBy that returns mulitple if there is not a single clear maximum.
/// </summary>
public static class ExtensionMethods {
    /// <summary>
    /// Variant of MaxBy that returns mulitple if there is not a single clear maximum.
    /// </summary>
    public static IEnumerable<TItem> AllMaxBy<TItem, TProperty>(this IEnumerable<TItem> items, Func<TItem, TProperty> getter)
    where TProperty : IEquatable<TProperty> {
        var maxPropVal = items.Max(getter);
        foreach (var item in items) {
            var itemPropVal = getter(item);
            if (Equals(itemPropVal, maxPropVal)) {
                yield return item;
            }
        }
    }

    public static IEnumerable<TItem> AllMinBy<TItem, TProperty>(this IEnumerable<TItem> items, Func<TItem, TProperty> getter)
    where TProperty : IEquatable<TProperty> {
        var minPropVal = items.Min(getter);
        foreach (var item in items) {
            var itemPropVal = getter(item);
            if (Equals(itemPropVal, minPropVal)) {
                yield return item;
            }
        }
    }

    extension(int value) {
        public sbyte AsSByte
        => (sbyte)value;
    }

    extension(float value) {
        public sbyte FloorAsSByte
        => (sbyte)value;
    }

    extension(double value) {
        public sbyte FloorAsSByte
        => (sbyte)value;
    }

    /// <summary>
    /// Since we need copy-constructors for Monte Carlo simulations, we're using
    /// the ObjectModel <see cref="ImmutableObjectAttribute"/> to mark the
    /// objects as immutable. This is a little assertion method to confirm.
    /// </summary>
    /// <param name="obj"></param>
    extension(object obj) {
        public bool HasImmutableAttribute {
            get {
                var type = obj.GetType();
                if(_immutableTypesCache.TryGetValue(type, out bool hasImmutable)) {
                    return hasImmutable;
                } else {
                    hasImmutable = obj.GetType().GetCustomAttributes<ImmutableObjectAttribute>().Any(attr => attr.Immutable);
                    _immutableTypesCache[type] = hasImmutable;
                    return hasImmutable;
                }
            }
        }

        public void ConfirmHasImmutableAttribute() {
            if (!obj.HasImmutableAttribute) {
                
                throw new InvalidOperationException(
                    $"The object of type '{obj.GetType().Name}' is expected to be immutable. " 
                    + "Confirm that the object is immutable and apply the {nameof(ImmutableObjectAttribute)} to its class."
                );
            }
        }
    }

    public static Dictionary<Type, bool> _immutableTypesCache = new Dictionary<Type, bool>();

    public static IEnumerable<PlayerIndexed> ToPlayersIndexed(this IReadOnlyList<PlayerInfo> playerInfos) {
        for(var i = 0; i < playerInfos.Count; i+=1) {
            yield return new PlayerIndexed(playerInfos[i], i);
        }
    }

    public static IEnumerable<PlayerIndexed> ToPlayersIndexed(this IReadOnlyList<string> playerMarks) {
        for(var i = 0; i < playerMarks.Count; i+=1) {
            yield return new PlayerIndexed(new PlayerInfo(playerMarks[i]), i);
        }
    }
}