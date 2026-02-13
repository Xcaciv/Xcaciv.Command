using Xunit;
using Xcaciv.Command;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xcaciv.Command.Tests
{
    public class EnvironmentContextEdgeCaseTests
    {
        /// <summary>
        /// Test: Case-insensitive environment variable access should work
        /// </summary>
        [Fact]
        public void CaseInsensitiveVariableAccess_ShouldWork()
        {
            // Arrange
            var env = new EnvironmentContext();
            env.SetValue("MyVariable", "value1");

            // Act
            var lowerResult = env.GetValue("myvariable");
            var upperResult = env.GetValue("MYVARIABLE");
            var mixedResult = env.GetValue("MyVariable");

            // Assert - All should return the same value
            Assert.Equal("value1", lowerResult);
            Assert.Equal("value1", upperResult);
            Assert.Equal("value1", mixedResult);
        }

        /// <summary>
        /// Test: Environment variable collision (same var, different case) should resolve to single value
        /// </summary>
        [Fact]
        public void VariableCollisionDifferentCase_ShouldResolveSafely()
        {
            // Arrange
            var env = new EnvironmentContext();
            env.SetValue("MyVar", "first");
            
            // Act - Try to retrieve the same variable with different cases
            var result1 = env.GetValue("MYVAR", "", storeDefault: false);
            
            // Assert
            Assert.Equal("first", result1);
        }

        /// <summary>
        /// Test: Child environment should be isolated from parent
        /// </summary>
        [Fact]
        public async Task ChildEnvironmentIsolation_ShouldNotAffectParentAsync()
        {
            // Arrange
            var parent = new EnvironmentContext();
            parent.SetValue("ParentVar", "parentValue");

            // Act
            var child = await parent.GetChild();
            child.SetValue("ChildVar", "childValue");

            // Assert
            Assert.Equal("parentValue", parent.GetValue("ParentVar"));
            Assert.Equal("", parent.GetValue("ChildVar")); // Child variable not in parent
            Assert.Equal("childValue", child.GetValue("ChildVar"));
        }

        /// <summary>
        /// Test: Child should inherit parent values but modifications shouldn't affect parent
        /// </summary>
        [Fact]
        public async Task ChildInheritsParentButModificationsIsolated_ShouldWorkAsync()
        {
            // Arrange
            var parent = new EnvironmentContext();
            parent.SetValue("Shared", "parentValue");

            // Act
            var child = await parent.GetChild();
            child.SetValue("Shared", "childValue");
            var parentValueAfter = parent.GetValue("Shared");
            var childValueAfter = child.GetValue("Shared");

            // Assert - Parent should be unaffected
            Assert.Equal("parentValue", parentValueAfter);
            Assert.Equal("childValue", childValueAfter);
        }

        /// <summary>
        /// Test: Parent modification after child creation should not affect child
        /// </summary>
        [Fact]
        public async Task ParentModificationAfterChildCreation_ShouldNotAffectChildAsync()
        {
            // Arrange
            var parent = new EnvironmentContext();
            parent.SetValue("InitialVar", "initialValue");

            // Act
            var child = await parent.GetChild();
            var childValueBefore = child.GetValue("InitialVar");

            parent.SetValue("NewVar", "newValue");
            parent.SetValue("InitialVar", "modifiedValue");

            var childNewVarValue = child.GetValue("NewVar");
            var childInitialVarAfter = child.GetValue("InitialVar");

            // Assert
            Assert.Equal("initialValue", childValueBefore);
            Assert.Equal("", childNewVarValue); // New parent var not visible to child
            Assert.Equal("initialValue", childInitialVarAfter); // Parent change doesn't affect child's copy
        }

        /// <summary>
        /// Test: Empty string variable value should be stored and retrieved
        /// </summary>
        [Fact]
        public void EmptyStringVariableValue_ShouldBeStored()
        {
            // Arrange
            var env = new EnvironmentContext();

            // Act
            env.SetValue("EmptyVar", "");
            var result = env.GetValue("EmptyVar", "default");

            // Assert - Empty string should be returned, not the default
            Assert.Equal("", result);
        }

        /// <summary>
        /// Test: Variable removal (SetValue to empty) and default behavior
        /// </summary>
        [Fact]
        public void VariableRemovalViaEmpty_ShouldBehaveProperly()
        {
            // Arrange
            var env = new EnvironmentContext();
            env.SetValue("MyVar", "value");

            // Act
            env.SetValue("MyVar", "");
            var result = env.GetValue("MyVar", "default");

            // Assert
            Assert.Equal("", result); // Empty string is stored, not default
        }

        /// <summary>
        /// Test: GetValue with default should store it if configured
        /// </summary>
        [Fact]
        public void GetValueWithDefaultStoreDefault_ShouldStoreDefault()
        {
            // Arrange
            var env = new EnvironmentContext();

            // Act
            var result1 = env.GetValue("NonExistent", "defaultValue", storeDefault: true);
            var result2 = env.GetValue("NonExistent", "otherDefault", storeDefault: true);

            // Assert - Default was stored on first call
            Assert.Equal("defaultValue", result1);
            Assert.Equal("defaultValue", result2); // Should return stored value, not new default
        }

        /// <summary>
        /// Test: GetValue with storeDefault=false should not modify environment
        /// </summary>
        [Fact]
        public void GetValueWithoutStoreDefault_ShouldNotModifyEnvironment()
        {
            // Arrange
            var env = new EnvironmentContext();

            // Act
            var result1 = env.GetValue("NonExistent", "defaultValue", storeDefault: false);
            var result2 = env.GetValue("NonExistent", "otherDefault", storeDefault: false);

            // Assert - Different defaults returned, nothing stored
            Assert.Equal("defaultValue", result1);
            Assert.Equal("otherDefault", result2);
            Assert.False(env.HasChanged); // No changes to environment
        }

        /// <summary>
        /// Test: HasChanged flag should track modifications
        /// </summary>
        [Fact]
        public void HasChangedFlag_ShouldTrackModifications()
        {
            // Arrange
            var env = new EnvironmentContext();
            Assert.False(env.HasChanged);

            // Act
            env.SetValue("Var", "value");

            // Assert
            Assert.True(env.HasChanged);
        }

        /// <summary>
        /// Test: Special characters in variable values should be preserved
        /// </summary>
        [Fact]
        public void SpecialCharactersInVariableValues_ShouldBePreserved()
        {
            // Arrange
            var env = new EnvironmentContext();
            var specialValue = "value!@#$%^&*(){}[]|\\:;\"'<>,.?/";

            // Act
            env.SetValue("SpecialVar", specialValue);
            var result = env.GetValue("SpecialVar");

            // Assert
            Assert.Equal(specialValue, result);
        }

        /// <summary>
        /// Test: Unicode characters in variable values should be preserved
        /// </summary>
        [Fact]
        public void UnicodeCharactersInVariableValues_ShouldBePreserved()
        {
            // Arrange
            var env = new EnvironmentContext();
            var unicodeValue = "Hello \u00A9 \u03A9 \u20AC \u4F60\u597D \uD83D\uDE03";

            // Act
            env.SetValue("UnicodeVar", unicodeValue);
            var result = env.GetValue("UnicodeVar");

            // Assert
            Assert.Equal(unicodeValue, result);
        }

        /// <summary>
        /// Test: Very long variable values should be handled
        /// </summary>
        [Fact]
        public void VeryLongVariableValue_ShouldBeHandled()
        {
            // Arrange
            var env = new EnvironmentContext();
            var longValue = new string('a', 10000);

            // Act
            env.SetValue("LongVar", longValue);
            var result = env.GetValue("LongVar");

            // Assert
            Assert.Equal(longValue, result);
            Assert.Equal(10000, result.Length);
        }

        /// <summary>
        /// Test: Multiple simultaneous child environments from same parent
        /// </summary>
        [Fact]
        public async Task MultipleSiblings_ShouldBeIndependentAsync()
        {
            // Arrange
            var parent = new EnvironmentContext();
            parent.SetValue("Shared", "parentValue");

            // Act
            var child1 = await parent.GetChild();
            var child2 = await parent.GetChild();

            child1.SetValue("Shared", "child1Value");
            child2.SetValue("Shared", "child2Value");

            // Assert
            Assert.Equal("parentValue", parent.GetValue("Shared"));
            Assert.Equal("child1Value", child1.GetValue("Shared"));
            Assert.Equal("child2Value", child2.GetValue("Shared"));
        }

        /// <summary>
        /// Test: Environment context created with initial dictionary
        /// </summary>
        [Fact]
        public void EnvironmentContextWithInitialDictionary_ShouldLoadValues()
        {
            // Arrange
            var initialDict = new Dictionary<string, string>
            {
                { "Var1", "value1" },
                { "Var2", "value2" },
                { "Var3", "value3" }
            };

            // Act
            var env = new EnvironmentContext(initialDict);
            var result1 = env.GetValue("var1");
            var result2 = env.GetValue("var2");
            var result3 = env.GetValue("var3");

            // Assert
            Assert.Equal("value1", result1);
            Assert.Equal("value2", result2);
            Assert.Equal("value3", result3);
        }
    }
}
