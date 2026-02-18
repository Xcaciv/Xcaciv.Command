using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using Xcaciv.Command;
using Xcaciv.Command.Interface;
using Xunit;

namespace Xcaciv.Command.UnitTests
{
    /// <summary>
    /// Unit tests for the ControllerEnvironmentContext class.
    /// </summary>
    public partial class ControllerEnvironmentContextTests
    {
        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext parameter assigns the environment correctly.
        /// Input: A valid mocked IEnvironmentContext.
        /// Expected: The provided environment is assigned and accessible through GetEnvironment method.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_AssignsEnvironmentCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var expectedDictionary = new Dictionary<string, string>
            {
                { "KEY1", "Value1" },
                { "KEY2", "Value2" }
            };
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(expectedDictionary);

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var result = context.GetEnvironment();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDictionary, result);
            mockEnvironment.Verify(e => e.GetEnvironment(), Times.Once);
        }

        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext initializes all other properties to default values.
        /// Input: A valid mocked IEnvironmentContext.
        /// Expected: Id is non-empty, Name has default value, Parent is null, and internal collections are initialized.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_InitializesOtherPropertiesToDefaults()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Assert
            Assert.NotEqual(Guid.Empty, context.Id);
            Assert.Equal("Controller Environment", context.Name);
            Assert.Null(context.Parent);
        }

        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext generates unique Id for each instance.
        /// Input: Two separate constructor calls with different mocked environments.
        /// Expected: Each instance has a unique, non-empty Guid.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_GeneratesUniqueId()
        {
            // Arrange
            var mockEnvironment1 = new Mock<IEnvironmentContext>();
            var mockEnvironment2 = new Mock<IEnvironmentContext>();

            // Act
            var context1 = new ControllerEnvironmentContext(mockEnvironment1.Object);
            var context2 = new ControllerEnvironmentContext(mockEnvironment2.Object);

            // Assert
            Assert.NotEqual(Guid.Empty, context1.Id);
            Assert.NotEqual(Guid.Empty, context2.Id);
            Assert.NotEqual(context1.Id, context2.Id);
        }

        /// <summary>
        /// Test: Constructor with null IEnvironmentContext parameter assigns null without throwing.
        /// Input: null for IEnvironmentContext parameter.
        /// Expected: Constructor completes without throwing, but GetEnvironment will throw NullReferenceException.
        /// </summary>
        [Fact]
        public void Constructor_WithNullEnvironment_AssignsNullWithoutThrowing()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext(null!);

            // Assert
            Assert.NotNull(context);
            Assert.NotEqual(Guid.Empty, context.Id);
            Assert.Throws<NullReferenceException>(() => context.GetEnvironment());
        }

        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext allows Name property to be modified.
        /// Input: A valid mocked IEnvironmentContext, then modify Name property.
        /// Expected: Name property can be set to a new value.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_AllowsNamePropertyModification()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            const string newName = "Custom Name";

            // Act
            context.Name = newName;

            // Assert
            Assert.Equal(newName, context.Name);
        }

        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext allows Parent property to be set.
        /// Input: A valid mocked IEnvironmentContext, then set Parent property.
        /// Expected: Parent property can be set to a Guid value.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_AllowsParentPropertyModification()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var parentGuid = Guid.NewGuid();

            // Act
            context.Parent = parentGuid;

            // Assert
            Assert.Equal(parentGuid, context.Parent);
        }

        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext delegates UpdateEnvironment call correctly.
        /// Input: A valid mocked IEnvironmentContext, then call UpdateEnvironment.
        /// Expected: The UpdateEnvironment call is forwarded to the injected environment.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_DelegatesUpdateEnvironmentCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var updateDictionary = new Dictionary<string, string>
            {
                { "NEW_KEY", "New_Value" }
            };

            // Act
            context.UpdateEnvironment(updateDictionary);

            // Assert
            mockEnvironment.Verify(e => e.UpdateEnvironment(updateDictionary), Times.Once);
        }

        /// <summary>
        /// Test: Constructor with valid IEnvironmentContext delegates SetAuditLogger call correctly.
        /// Input: A valid mocked IEnvironmentContext and mocked IAuditLogger.
        /// Expected: The SetAuditLogger call is forwarded to the injected environment.
        /// </summary>
        [Fact]
        public void Constructor_WithValidEnvironment_DelegatesSetAuditLoggerCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockAuditLogger = new Mock<IAuditLogger>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Act
            context.SetAuditLogger(mockAuditLogger.Object);

            // Assert
            mockEnvironment.Verify(e => e.SetAuditLogger(mockAuditLogger.Object), Times.Once);
        }

        /// <summary>
        /// Tests that the HasChanged property getter correctly returns the logical OR of the backing field and _environment.HasChanged.
        /// Verifies all four combinations of backing field and environment HasChanged values.
        /// </summary>
        /// <param name="backingFieldValue">The value to set for the backing field via the HasChanged setter.</param>
        /// <param name="environmentHasChanged">The value returned by the mocked environment's HasChanged property.</param>
        /// <param name="expectedResult">The expected result of the HasChanged getter.</param>
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void HasChanged_WithVariousBackingFieldAndEnvironmentValues_ReturnsExpectedResult(
            bool backingFieldValue,
            bool environmentHasChanged,
            bool expectedResult)
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(environmentHasChanged);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            if (backingFieldValue)
            {
                context.HasChanged = true;
            }

            // Act
            var result = context.HasChanged;

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Tests that the HasChanged property setter correctly updates the backing field.
        /// Verifies that the setter stores the provided value and the getter reflects it when environment.HasChanged is false.
        /// </summary>
        /// <param name="value">The value to set for the HasChanged property.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HasChanged_WhenSetToValue_UpdatesBackingField(bool value)
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Act
            context.HasChanged = value;

            // Assert
            Assert.Equal(value, context.HasChanged);
        }

        /// <summary>
        /// Tests that setting HasChanged to false does not override environment.HasChanged when it is true.
        /// Verifies that the getter still returns true when environment.HasChanged is true, regardless of the backing field value.
        /// </summary>
        [Fact]
        public void HasChanged_WhenSetToFalseButEnvironmentHasChangedIsTrue_ReturnsTrue()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(true);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Act
            context.HasChanged = false;
            var result = context.HasChanged;

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that setting HasChanged multiple times correctly updates the backing field each time.
        /// Verifies that the setter can be called repeatedly with different values and the getter reflects the most recent value.
        /// </summary>
        [Fact]
        public void HasChanged_WhenSetMultipleTimes_ReflectsLatestValue()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Act & Assert
            context.HasChanged = true;
            Assert.True(context.HasChanged);

            context.HasChanged = false;
            Assert.False(context.HasChanged);

            context.HasChanged = true;
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: Parameterless constructor creates a valid instance
        /// Input: No parameters
        /// Expected: Instance is created successfully and is not null
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_CreatesValidInstance()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext();

            // Assert
            Assert.NotNull(context);
        }

        /// <summary>
        /// Test: Parameterless constructor initializes Id property with a non-empty Guid
        /// Input: No parameters
        /// Expected: Id property is not Guid.Empty
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_InitializesIdWithNonEmptyGuid()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext();

            // Assert
            Assert.NotEqual(Guid.Empty, context.Id);
        }

        /// <summary>
        /// Test: Parameterless constructor initializes Name property with default value
        /// Input: No parameters
        /// Expected: Name property is set to "Controller Envirnonment"
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_InitializesNameWithDefaultValue()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext();

            // Assert
            Assert.Equal("Controller Environment", context.Name);
        }

        /// <summary>
        /// Test: Parameterless constructor initializes Parent property as null
        /// Input: No parameters
        /// Expected: Parent property is null
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_InitializesParentAsNull()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext();

            // Assert
            Assert.Null(context.Parent);
        }

        /// <summary>
        /// Test: Multiple instances created via parameterless constructor have unique Ids
        /// Input: No parameters
        /// Expected: Each instance has a different Id
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_CreatesUniqueIdsForMultipleInstances()
        {
            // Arrange & Act
            var context1 = new ControllerEnvironmentContext();
            var context2 = new ControllerEnvironmentContext();
            var context3 = new ControllerEnvironmentContext();

            // Assert
            Assert.NotEqual(context1.Id, context2.Id);
            Assert.NotEqual(context2.Id, context3.Id);
            Assert.NotEqual(context1.Id, context3.Id);
        }

        /// <summary>
        /// Test: Parameterless constructor initializes HasChanged property with default value
        /// Input: No parameters
        /// Expected: HasChanged is false by default
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_InitializesHasChangedAsFalse()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext();

            // Assert
            Assert.False(context.HasChanged);
        }

        /// <summary>
        /// Test: GetEnvironment method works after parameterless constructor
        /// Input: No parameters
        /// Expected: GetEnvironment returns a non-null dictionary
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_GetEnvironmentWorksImmediately()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();

            // Act
            var environment = context.GetEnvironment();

            // Assert
            Assert.NotNull(environment);
        }

        /// <summary>
        /// Test: GetEnvironment with command name returns empty dictionary for non-existent command
        /// Input: Command name "testCommand"
        /// Expected: Returns empty dictionary
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_GetEnvironmentWithCommandNameReturnsEmptyDictionary()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();

            // Act
            var environment = context.GetEnvironment("testCommand", false);

            // Assert
            Assert.NotNull(environment);
            Assert.Empty(environment);
        }

        /// <summary>
        /// Test: DisposeAsync completes successfully after parameterless constructor
        /// Input: No parameters
        /// Expected: DisposeAsync completes without exceptions
        /// </summary>
        [Fact]
        public async Task ControllerEnvironmentContext_DefaultConstructor_DisposeAsyncCompletesSuccessfully()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();

            // Act
            var disposeTask = context.DisposeAsync();

            // Assert
            Assert.True(disposeTask.IsCompleted);
            await disposeTask;
        }

        /// <summary>
        /// Test: Instance created via parameterless constructor can be used with CommandController
        /// Input: No parameters
        /// Expected: Instance is compatible with CommandController usage
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_InstanceIsCompatibleWithCommandController()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext();
            IControllerEnvironmentContext interfaceContext = context;

            // Assert
            Assert.NotNull(interfaceContext);
            Assert.Same(context, interfaceContext);
        }

        /// <summary>
        /// Test: Name property can be modified after parameterless constructor
        /// Input: No parameters, then set Name to "Custom Name"
        /// Expected: Name property is updated successfully
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_NamePropertyCanBeModified()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();
            var newName = "Custom Name";

            // Act
            context.Name = newName;

            // Assert
            Assert.Equal(newName, context.Name);
        }

        /// <summary>
        /// Test: Parent property can be set after parameterless constructor
        /// Input: No parameters, then set Parent to a new Guid
        /// Expected: Parent property is updated successfully
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_ParentPropertyCanBeSet()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();
            var parentId = Guid.NewGuid();

            // Act
            context.Parent = parentId;

            // Assert
            Assert.Equal(parentId, context.Parent);
        }

        /// <summary>
        /// Test: Multiple concurrent instantiations via parameterless constructor succeed
        /// Input: No parameters, multiple concurrent calls
        /// Expected: All instances are created with unique Ids without exceptions
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_ConcurrentInstantiationSucceeds()
        {
            // Arrange
            var instances = new System.Collections.Concurrent.ConcurrentBag<ControllerEnvironmentContext>();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var context = new ControllerEnvironmentContext();
                    instances.Add(context);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(100, instances.Count);
            var ids = new HashSet<Guid>();
            foreach (var instance in instances)
            {
                Assert.NotEqual(Guid.Empty, instance.Id);
                ids.Add(instance.Id);
            }
            Assert.Equal(100, ids.Count); // All Ids are unique
        }

        /// <summary>
        /// Test: UpdateEnvironment can be called on instance created via parameterless constructor
        /// Input: Empty dictionary
        /// Expected: Method executes without exception
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_UpdateEnvironmentCanBeCalled()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();
            var dictionary = new Dictionary<string, string>();

            // Act
            context.UpdateEnvironment(dictionary);

            // Assert - No exception thrown
            Assert.NotNull(context);
        }

        /// <summary>
        /// Test: SetAuditLogger can be called on instance created via parameterless constructor
        /// Input: Mocked IAuditLogger
        /// Expected: Method executes without exception
        /// </summary>
        [Fact]
        public void ControllerEnvironmentContext_DefaultConstructor_SetAuditLoggerCanBeCalled()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();
            var mockAuditLogger = new Mock<IAuditLogger>();

            // Act
            context.SetAuditLogger(mockAuditLogger.Object);

            // Assert - No exception thrown
            Assert.NotNull(context);
        }

        /// <summary>
        /// Tests that DisposeAsync returns a completed ValueTask for a default instance.
        /// Input: Default ControllerEnvironmentContext instance.
        /// Expected: ValueTask is completed and no exception is thrown.
        /// </summary>
        [Fact]
        public async Task DisposeAsync_DefaultInstance_ReturnsCompletedTask()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();

            // Act
            var result = context.DisposeAsync();

            // Assert
            Assert.True(result.IsCompleted);
            await result;
        }

        /// <summary>
        /// Tests that DisposeAsync can be called multiple times without throwing exceptions.
        /// Input: ControllerEnvironmentContext instance, DisposeAsync called twice.
        /// Expected: Both calls complete successfully without exception.
        /// </summary>
        [Fact]
        public async Task DisposeAsync_CalledMultipleTimes_CompletesSuccessfully()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();

            // Act
            await context.DisposeAsync();
            var secondResult = context.DisposeAsync();

            // Assert
            Assert.True(secondResult.IsCompleted);
            await secondResult;
        }

        /// <summary>
        /// Tests that DisposeAsync returns a completed ValueTask when initialized with an environment.
        /// Input: ControllerEnvironmentContext with IEnvironmentContext.
        /// Expected: ValueTask is completed and no exception is thrown.
        /// </summary>
        [Fact]
        public async Task DisposeAsync_WithEnvironment_ReturnsCompletedTask()
        {
            // Arrange
            var environment = new EnvironmentContext();
            var context = new ControllerEnvironmentContext(environment);

            // Act
            var result = context.DisposeAsync();

            // Assert
            Assert.True(result.IsCompleted);
            await result;
        }

        /// <summary>
        /// Tests that DisposeAsync returns a completed ValueTask when initialized with environment and command environment.
        /// Input: ControllerEnvironmentContext with IEnvironmentContext and ConcurrentDictionary.
        /// Expected: ValueTask is completed and no exception is thrown.
        /// </summary>
        [Fact]
        public async Task DisposeAsync_WithEnvironmentAndCommandEnvironment_ReturnsCompletedTask()
        {
            // Arrange
            var environment = new EnvironmentContext();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(environment, commandEnvironment);

            // Act
            var result = context.DisposeAsync();

            // Assert
            Assert.True(result.IsCompleted);
            await result;
        }

        /// <summary>
        /// Tests that DisposeAsync completes synchronously.
        /// Input: Default ControllerEnvironmentContext instance.
        /// Expected: ValueTask is completed synchronously without async execution.
        /// </summary>
        [Fact]
        public void DisposeAsync_DefaultInstance_CompleteSynchronously()
        {
            // Arrange
            var context = new ControllerEnvironmentContext();

            // Act
            var result = context.DisposeAsync();

            // Assert
            Assert.True(result.IsCompletedSuccessfully);
            Assert.True(result.IsCompleted);
        }

        /// <summary>
        /// Test: GetChild with empty command environment should return child context without setting any values
        /// Input: commandName exists but has no environment variables
        /// Expected: Child context created, no SetValue calls made
        /// </summary>
        [Fact]
        public async Task GetChild_EmptyCommandEnvironment_ReturnsChildWithoutSettingValues()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            commandEnvironment["test"] = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            Assert.Same(mockChild.Object, result);
            mockChild.Verify(c => c.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: GetChild with keys without prefix should add prefix to all keys
        /// Input: commandName="test", keys without "test_" prefix
        /// Expected: All keys prefixed with "test_" and set on child context
        /// </summary>
        [Fact]
        public async Task GetChild_KeysWithoutPrefix_AddsPrefix()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            commandEnvironment["test"] = testEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("test_key1", "value1"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_key2", "value2"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with keys already having prefix should not duplicate prefix
        /// Input: commandName="test", keys already with "test_" prefix (exact case)
        /// Expected: Keys remain unchanged, no duplicate prefix added
        /// </summary>
        [Fact]
        public async Task GetChild_KeysWithExactPrefix_DoesNotDuplicatePrefix()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test_key1"] = "value1",
                ["test_key2"] = "value2"
            };
            commandEnvironment["test"] = testEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("test_key1", "value1"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_key2", "value2"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_test_key1", It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: GetChild with keys having case-insensitive prefix match should not duplicate prefix
        /// Input: commandName="test", keys with "TEST_" or "TeSt_" prefix (different case)
        /// Expected: Keys remain unchanged due to OrdinalIgnoreCase comparison
        /// </summary>
        [Fact]
        public async Task GetChild_KeysWithCaseInsensitivePrefix_DoesNotDuplicatePrefix()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TEST_key1"] = "value1",
                ["TeSt_key2"] = "value2"
            };
            commandEnvironment["test"] = testEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("TEST_key1", "value1"), Times.Once);
            mockChild.Verify(c => c.SetValue("TeSt_key2", "value2"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_TEST_key1", It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: GetChild with mixed keys (some with prefix, some without) should handle correctly
        /// Input: commandName="test", mix of prefixed and non-prefixed keys
        /// Expected: Prefixed keys unchanged, non-prefixed keys get prefix added
        /// </summary>
        [Fact]
        public async Task GetChild_MixedKeys_HandlesCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test_prefixed"] = "value1",
                ["notprefixed"] = "value2",
                ["TEST_caseinsensitive"] = "value3"
            };
            commandEnvironment["test"] = testEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("test_prefixed", "value1"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_notprefixed", "value2"), Times.Once);
            mockChild.Verify(c => c.SetValue("TEST_caseinsensitive", "value3"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with null commandName should handle gracefully
        /// Input: commandName is null
        /// Expected: Returns child context, processes base environment with "_" prefix
        /// </summary>
        [Fact]
        public async Task GetChild_NullCommandName_HandlesGracefully()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            var baseEnvironment = new Dictionary<string, string>
            {
                ["key1"] = "value1"
            };

            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(baseEnvironment);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild(null!);

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("_key1", "value1"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with empty commandName should use underscore as prefix
        /// Input: commandName is empty string
        /// Expected: Returns child context with "_" prefix added to keys
        /// </summary>
        [Fact]
        public async Task GetChild_EmptyCommandName_UsesUnderscorePrefix()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            var baseEnvironment = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };

            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(baseEnvironment);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild(string.Empty);

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("_key1", "value1"), Times.Once);
            mockChild.Verify(c => c.SetValue("_key2", "value2"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with whitespace commandName should use whitespace with underscore as prefix
        /// Input: commandName is "  " (whitespace)
        /// Expected: Returns child context with "  _" prefix
        /// </summary>
        [Fact]
        public async Task GetChild_WhitespaceCommandName_UsesWhitespacePrefix()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var whitespaceEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = "value1"
            };
            commandEnvironment["  "] = whitespaceEnv;

            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("  ");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("  _key1", "value1"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with non-existent commandName should return empty environment
        /// Input: commandName that doesn't exist in _commandEnvironment
        /// Expected: Returns child context without setting any values
        /// </summary>
        [Fact]
        public async Task GetChild_NonExistentCommandName_ReturnsChildWithoutValues()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            commandEnvironment["test"] = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = "value1"
            };

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("nonexistent");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: Multiple calls to GetChild should return isolated child contexts
        /// Input: Multiple sequential calls with different command names
        /// Expected: Each call returns a distinct child context from the mocked environment
        /// </summary>
        [Fact]
        public async Task GetChild_MultipleCalls_ReturnsIsolatedChildren()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild1 = new Mock<IEnvironmentContext>();
            var mockChild2 = new Mock<IEnvironmentContext>();

            var callCount = 0;
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? mockChild1.Object : mockChild2.Object;
            });

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            commandEnvironment["cmd1"] = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = "value1"
            };
            commandEnvironment["cmd2"] = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key2"] = "value2"
            };

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result1 = await context.GetChild("cmd1");
            var result2 = await context.GetChild("cmd2");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotSame(result1, result2);
            mockChild1.Verify(c => c.SetValue("cmd1_key1", "value1"), Times.Once);
            mockChild2.Verify(c => c.SetValue("cmd2_key2", "value2"), Times.Once);
            mockChild1.Verify(c => c.SetValue("cmd2_key2", It.IsAny<string>()), Times.Never);
            mockChild2.Verify(c => c.SetValue("cmd1_key1", It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: GetChild with special characters in commandName should handle correctly
        /// Input: commandName with special characters like "test-cmd", "test.cmd"
        /// Expected: Prefix created with special characters, keys prefixed correctly
        /// </summary>
        [Theory]
        [InlineData("test-cmd", "test-cmd_key1")]
        [InlineData("test.cmd", "test.cmd_key1")]
        [InlineData("test@cmd", "test@cmd_key1")]
        [InlineData("test$cmd", "test$cmd_key1")]
        public async Task GetChild_SpecialCharactersInCommandName_HandlesCorrectly(string commandName, string expectedKey)
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var cmdEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = "value1"
            };
            commandEnvironment[commandName] = cmdEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild(commandName);

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue(expectedKey, "value1"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with very long commandName should handle correctly
        /// Input: commandName with 1000+ characters
        /// Expected: Prefix created successfully, keys prefixed correctly
        /// </summary>
        [Fact]
        public async Task GetChild_VeryLongCommandName_HandlesCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var longCommandName = new string('a', 1000);
            var expectedKey = longCommandName + "_key1";

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var cmdEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = "value1"
            };
            commandEnvironment[longCommandName] = cmdEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild(longCommandName);

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue(expectedKey, "value1"), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with empty value strings should set them correctly
        /// Input: Command environment with empty string values
        /// Expected: Empty values are set on child context
        /// </summary>
        [Fact]
        public async Task GetChild_EmptyValues_SetsCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key1"] = string.Empty,
                ["key2"] = ""
            };
            commandEnvironment["test"] = testEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("test_key1", string.Empty), Times.Once);
            mockChild.Verify(c => c.SetValue("test_key2", ""), Times.Once);
        }

        /// <summary>
        /// Test: GetChild with keys containing special characters should handle correctly
        /// Input: Keys with special characters, spaces, symbols
        /// Expected: Keys prefixed correctly regardless of special characters
        /// </summary>
        [Fact]
        public async Task GetChild_KeysWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var mockChild = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetChild()).ReturnsAsync(mockChild.Object);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key-with-dash"] = "value1",
                ["key.with.dot"] = "value2",
                ["key with space"] = "value3"
            };
            commandEnvironment["test"] = testEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = await context.GetChild("test");

            // Assert
            Assert.NotNull(result);
            mockChild.Verify(c => c.SetValue("test_key-with-dash", "value1"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_key.with.dot", "value2"), Times.Once);
            mockChild.Verify(c => c.SetValue("test_key with space", "value3"), Times.Once);
        }

        /// <summary>
        /// Tests that GetEnvironment returns the general environment dictionary when commandName is null.
        /// Input: commandName = null
        /// Expected: Returns the dictionary from _environment.GetEnvironment() and verifies the method was called.
        /// </summary>
        [Fact]
        public void GetEnvironment_NullCommandName_ReturnsEnvironmentDictionary()
        {
            // Arrange
            var expectedDict = new Dictionary<string, string>
            {
                { "KEY1", "VALUE1" },
                { "KEY2", "VALUE2" }
            };
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(expectedDict);

            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Act
            var result = context.GetEnvironment(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedDict, result);
            mockEnvironment.Verify(e => e.GetEnvironment(), Times.Once);
        }

        /// <summary>
        /// Tests that GetEnvironment returns the general environment dictionary when commandName is an empty string.
        /// Input: commandName = ""
        /// Expected: Returns the dictionary from _environment.GetEnvironment() and verifies the method was called.
        /// </summary>
        [Fact]
        public void GetEnvironment_EmptyCommandName_ReturnsEnvironmentDictionary()
        {
            // Arrange
            var expectedDict = new Dictionary<string, string>
            {
                { "PATH", "/usr/bin" },
                { "HOME", "/home/user" }
            };
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(expectedDict);

            var context = new ControllerEnvironmentContext(mockEnvironment.Object);

            // Act
            var result = context.GetEnvironment(string.Empty);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedDict, result);
            mockEnvironment.Verify(e => e.GetEnvironment(), Times.Once);
        }

        /// <summary>
        /// Tests that GetEnvironment returns an empty dictionary when commandName is whitespace and not in _commandEnvironment.
        /// Input: commandName = "   " (whitespace only)
        /// Expected: Returns an empty dictionary (whitespace is not treated as null/empty by string.IsNullOrEmpty).
        /// </summary>
        [Fact]
        public void GetEnvironment_WhitespaceCommandName_ReturnsEmptyDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment("   ", false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            mockEnvironment.Verify(e => e.GetEnvironment(), Times.Never);
        }

        /// <summary>
        /// Tests that GetEnvironment returns a copy of the command-specific environment when commandName exists.
        /// Input: commandName exists in _commandEnvironment with values
        /// Expected: Returns a new Dictionary containing the same key-value pairs as the command environment.
        /// </summary>
        [Fact]
        public void GetEnvironment_ExistingCommandName_ReturnsCopyOfCommandEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var commandEnv = new ConcurrentDictionary<string, string>
            {
                ["VAR1"] = "Value1",
                ["VAR2"] = "Value2"
            };
            commandEnvironment["TestCommand"] = commandEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment("TestCommand", false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Value1", result["VAR1"]);
            Assert.Equal("Value2", result["VAR2"]);
            Assert.NotSame(commandEnv, result); // Verify it's a copy, not the same reference
            mockEnvironment.Verify(e => e.GetEnvironment(), Times.Never);
        }

        /// <summary>
        /// Tests that GetEnvironment returns an empty dictionary when commandName does not exist in _commandEnvironment.
        /// Input: commandName = "NonExistentCommand"
        /// Expected: Returns a new empty Dictionary.
        /// </summary>
        [Fact]
        public void GetEnvironment_NonExistentCommandName_ReturnsEmptyDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment("NonExistentCommand", false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            mockEnvironment.Verify(e => e.GetEnvironment(), Times.Never);
        }

        /// <summary>
        /// Tests that GetEnvironment is case-insensitive when looking up commandName.
        /// Input: commandName with different casing than stored key
        /// Expected: Returns the command environment regardless of case.
        /// </summary>
        [Theory]
        [InlineData("testcommand")]
        [InlineData("TESTCOMMAND")]
        [InlineData("TestCommand")]
        [InlineData("tEsTcOmMaNd")]
        public void GetEnvironment_CaseInsensitiveCommandName_ReturnsCommandEnvironment(string commandName)
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var commandEnv = new ConcurrentDictionary<string, string>
            {
                ["KEY"] = "VALUE"
            };
            commandEnvironment["TestCommand"] = commandEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment(commandName, false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("VALUE", result["KEY"]);
        }

        /// <summary>
        /// Tests that GetEnvironment handles very long commandName strings correctly.
        /// Input: commandName = very long string (1000 characters)
        /// Expected: Returns an empty dictionary if not found in _commandEnvironment.
        /// </summary>
        [Fact]
        public void GetEnvironment_VeryLongCommandName_ReturnsEmptyDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var veryLongCommandName = new string('a', 1000);

            // Act
            var result = context.GetEnvironment(veryLongCommandName);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetEnvironment handles commandName with special characters.
        /// Input: commandName with special characters
        /// Expected: Returns empty dictionary or found environment based on whether key exists.
        /// </summary>
        [Theory]
        [InlineData("command-with-dashes")]
        [InlineData("command_with_underscores")]
        [InlineData("command.with.dots")]
        [InlineData("command$with$special")]
        public void GetEnvironment_SpecialCharactersInCommandName_HandlesCorrectly(string commandName)
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment(commandName);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that modifications to the returned dictionary do not affect the original command environment.
        /// Input: commandName exists in _commandEnvironment
        /// Expected: Modifying returned dictionary does not change the original environment.
        /// </summary>
        [Fact]
        public void GetEnvironment_ModifyingReturnedDictionary_DoesNotAffectOriginal()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var commandEnv = new ConcurrentDictionary<string, string>
            {
                ["ORIGINAL"] = "OriginalValue"
            };
            commandEnvironment["TestCommand"] = commandEnv;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment("TestCommand", false);
            result["ORIGINAL"] = "ModifiedValue";
            result["NEW"] = "NewValue";

            // Assert
            Assert.Equal("OriginalValue", commandEnv["ORIGINAL"]); // Original unchanged
            Assert.False(commandEnv.ContainsKey("NEW")); // New key not in original
        }

        /// <summary>
        /// Tests that GetEnvironment returns an empty dictionary when _commandEnvironment is empty.
        /// Input: commandName = "AnyCommand", _commandEnvironment is empty
        /// Expected: Returns a new empty Dictionary.
        /// </summary>
        [Fact]
        public void GetEnvironment_EmptyCommandEnvironment_ReturnsEmptyDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment("AnyCommand", false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetEnvironment handles multiple commands in _commandEnvironment correctly.
        /// Input: Multiple commands in _commandEnvironment, request specific one
        /// Expected: Returns only the requested command's environment.
        /// </summary>
        [Fact]
        public void GetEnvironment_MultipleCommandsInEnvironment_ReturnsCorrectOne()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            var command1Env = new ConcurrentDictionary<string, string> { ["CMD1_VAR"] = "Value1" };
            var command2Env = new ConcurrentDictionary<string, string> { ["CMD2_VAR"] = "Value2" };
            var command3Env = new ConcurrentDictionary<string, string> { ["CMD3_VAR"] = "Value3" };

            commandEnvironment["Command1"] = command1Env;
            commandEnvironment["Command2"] = command2Env;
            commandEnvironment["Command3"] = command3Env;

            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            var result = context.GetEnvironment("Command2", false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Value2", result["CMD2_VAR"]);
            Assert.False(result.ContainsKey("CMD1_VAR"));
            Assert.False(result.ContainsKey("CMD3_VAR"));
        }

        /// <summary>
        /// Test: SetValue with null commandName delegates to environment
        /// Input: key="TEST_KEY", value="value", commandName=null
        /// Expected: _environment.SetValue is called with key and value
        /// </summary>
        [Fact]
        public void SetValue_NullCommandName_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "TEST_KEY";
            var value = "testValue";

            // Act
            context.SetValue(key, value, null!);

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: SetValue with empty commandName delegates to environment
        /// Input: key="TEST_KEY", value="value", commandName=""
        /// Expected: _environment.SetValue is called with key and value
        /// </summary>
        [Fact]
        public void SetValue_EmptyCommandName_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "TEST_KEY";
            var value = "testValue";

            // Act
            context.SetValue(key, value, "");

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: SetValue with whitespace commandName but non-matching key delegates to environment
        /// Input: key="TEST_KEY", value="value", commandName=" "
        /// Expected: _environment.SetValue is called because key doesn't start with " _"
        /// </summary>
        [Fact]
        public void SetValue_WhitespaceCommandNameNonMatchingKey_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "TEST_KEY";
            var value = "testValue";

            // Act
            context.SetValue(key, value, " ");

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: SetValue with commandName but key not matching prefix delegates to environment
        /// Input: key="OTHER_KEY", value="value", commandName="TEST"
        /// Expected: _environment.SetValue is called because key doesn't start with "TEST_"
        /// </summary>
        [Fact]
        public void SetValue_KeyDoesNotMatchCommandPrefix_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "OTHER_KEY";
            var value = "testValue";

            // Act
            context.SetValue(key, value, "TEST");

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: SetValue with key matching command prefix adds to command-specific dictionary
        /// Input: key="TEST_MYVAR", value="value", commandName="TEST"
        /// Expected: Value is stored in command-specific dictionary, HasChanged is true
        /// </summary>
        [Fact]
        public void SetValue_KeyMatchesCommandPrefix_StoresInCommandDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var key = "TEST_MYVAR";
            var value = "testValue";
            var commandName = "TEST";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            Assert.True(commandEnvironment.ContainsKey(commandName));
            Assert.True(commandEnvironment[commandName].ContainsKey(key));
            Assert.Equal(value, commandEnvironment[commandName][key]);
            Assert.True(context.HasChanged);
            mockEnvironment.Verify(e => e.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: SetValue with key matching command prefix case-insensitively stores in command-specific dictionary
        /// Input: key="test_myvar", value="value", commandName="TEST"
        /// Expected: Value is stored in command-specific dictionary due to case-insensitive matching
        /// </summary>
        [Fact]
        public void SetValue_KeyMatchesCommandPrefixCaseInsensitive_StoresInCommandDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var key = "test_myvar";
            var value = "testValue";
            var commandName = "TEST";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            Assert.True(commandEnvironment.ContainsKey(commandName));
            Assert.True(commandEnvironment[commandName].ContainsKey(key));
            Assert.Equal(value, commandEnvironment[commandName][key]);
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: SetValue updating existing command-specific value overwrites and sets HasChanged
        /// Input: key="TEST_MYVAR" previously set to "oldValue", now set to "newValue", commandName="TEST"
        /// Expected: Value is updated in command-specific dictionary, HasChanged is true
        /// </summary>
        [Fact]
        public void SetValue_UpdatingExistingCommandValue_OverwritesValue()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var key = "TEST_MYVAR";
            var oldValue = "oldValue";
            var newValue = "newValue";
            var commandName = "TEST";

            // Act - First set
            context.SetValue(key, oldValue, commandName);
            // Act - Second set (update)
            context.SetValue(key, newValue, commandName);

            // Assert
            Assert.Equal(newValue, commandEnvironment[commandName][key]);
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: SetValue with multiple different commandNames maintains separate dictionaries
        /// Input: Multiple calls with different commandNames
        /// Expected: Each commandName has its own dictionary with correct values
        /// </summary>
        [Fact]
        public void SetValue_MultipleDifferentCommandNames_MaintainsSeparateDictionaries()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Act
            context.SetValue("CMD1_VAR", "value1", "CMD1");
            context.SetValue("CMD2_VAR", "value2", "CMD2");
            context.SetValue("CMD1_VAR2", "value3", "CMD1");

            // Assert
            Assert.Equal(2, commandEnvironment.Count);
            Assert.True(commandEnvironment.ContainsKey("CMD1"));
            Assert.True(commandEnvironment.ContainsKey("CMD2"));
            Assert.Equal("value1", commandEnvironment["CMD1"]["CMD1_VAR"]);
            Assert.Equal("value3", commandEnvironment["CMD1"]["CMD1_VAR2"]);
            Assert.Equal("value2", commandEnvironment["CMD2"]["CMD2_VAR"]);
        }

        /// <summary>
        /// Test: SetValue with empty string value stores empty string
        /// Input: key="TEST_KEY", value="", commandName="TEST"
        /// Expected: Empty string is stored in command-specific dictionary
        /// </summary>
        [Fact]
        public void SetValue_EmptyStringValue_StoresEmptyString()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var key = "TEST_KEY";
            var value = "";
            var commandName = "TEST";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            Assert.Equal(value, commandEnvironment[commandName][key]);
        }

        /// <summary>
        /// Test: SetValue with empty key and matching command prefix stores empty key
        /// Input: key="", value="value", commandName=""
        /// Expected: Delegates to environment due to empty commandName
        /// </summary>
        [Fact]
        public void SetValue_EmptyKey_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "";
            var value = "testValue";

            // Act
            context.SetValue(key, value, "");

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: SetValue with key exactly matching commandName without underscore delegates to environment
        /// Input: key="TEST", value="value", commandName="TEST"
        /// Expected: Delegates to environment because key doesn't start with "TEST_"
        /// </summary>
        [Fact]
        public void SetValue_KeyExactlyMatchesCommandNameWithoutUnderscore_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "TEST";
            var value = "testValue";
            var commandName = "TEST";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: SetValue with special characters in commandName and matching key
        /// Input: key="CMD-1_VAR", value="value", commandName="CMD-1"
        /// Expected: Value is stored in command-specific dictionary
        /// </summary>
        [Fact]
        public void SetValue_SpecialCharactersInCommandName_StoresInCommandDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var key = "CMD-1_VAR";
            var value = "testValue";
            var commandName = "CMD-1";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            Assert.True(commandEnvironment.ContainsKey(commandName));
            Assert.Equal(value, commandEnvironment[commandName][key]);
        }

        /// <summary>
        /// Test: SetValue with very long strings for key, value, and commandName
        /// Input: Very long strings
        /// Expected: Values are stored correctly
        /// </summary>
        [Fact]
        public void SetValue_VeryLongStrings_StoresCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var commandName = new string('C', 1000);
            var key = commandName + "_" + new string('K', 1000);
            var value = new string('V', 10000);

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            Assert.True(commandEnvironment.ContainsKey(commandName));
            Assert.Equal(value, commandEnvironment[commandName][key]);
        }

        /// <summary>
        /// Test: SetValue does not modify environment HasChanged when delegating to environment
        /// Input: key="TEST", value="value", commandName=""
        /// Expected: HasChanged reflects only environment's HasChanged status
        /// </summary>
        [Fact]
        public void SetValue_DelegatingToEnvironment_HasChangedReflectsEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.SetupGet(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "TEST";
            var value = "testValue";

            // Act
            context.SetValue(key, value, "");

            // Assert
            Assert.False(context.HasChanged);
        }

        /// <summary>
        /// Test: SetValue with commandName containing underscore and matching key
        /// Input: key="TEST_CMD_VAR", value="value", commandName="TEST_CMD"
        /// Expected: Value is stored in command-specific dictionary
        /// </summary>
        [Fact]
        public void SetValue_CommandNameWithUnderscore_StoresInCommandDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var key = "TEST_CMD_VAR";
            var value = "testValue";
            var commandName = "TEST_CMD";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            Assert.True(commandEnvironment.ContainsKey(commandName));
            Assert.Equal(value, commandEnvironment[commandName][key]);
        }

        /// <summary>
        /// Test: SetValue with key starting with commandName but missing underscore delegates to environment
        /// Input: key="TESTVAR", value="value", commandName="TEST"
        /// Expected: Delegates to environment because key doesn't start with "TEST_"
        /// </summary>
        [Fact]
        public void SetValue_KeyStartsWithCommandNameButMissingUnderscore_DelegatesToEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var key = "TESTVAR";
            var value = "testValue";
            var commandName = "TEST";

            // Act
            context.SetValue(key, value, commandName);

            // Assert
            mockEnvironment.Verify(e => e.SetValue(key, value), Times.Once);
        }

        /// <summary>
        /// Test: UpdateEnvironment with null commandName delegates to shared environment
        /// Input: dictionary with values, commandName = null
        /// Expected: Calls _environment.UpdateEnvironment with the dictionary, does not update command-specific environment
        /// </summary>
        [Fact]
        public void UpdateEnvironment_NullCommandName_DelegatesToSharedEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "VAR1", "value1" },
                { "VAR2", "value2" }
            };

            // Act
            context.UpdateEnvironment(dictionary, null!);

            // Assert
            mockEnvironment.Verify(e => e.UpdateEnvironment(dictionary), Times.Once);
        }

        /// <summary>
        /// Test: UpdateEnvironment with empty commandName delegates to shared environment
        /// Input: dictionary with values, commandName = empty string
        /// Expected: Calls _environment.UpdateEnvironment with the dictionary
        /// </summary>
        [Fact]
        public void UpdateEnvironment_EmptyCommandName_DelegatesToSharedEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "VAR1", "value1" },
                { "VAR2", "value2" }
            };

            // Act
            context.UpdateEnvironment(dictionary, string.Empty);

            // Assert
            mockEnvironment.Verify(e => e.UpdateEnvironment(dictionary), Times.Once);
        }

        /// <summary>
        /// Test: UpdateEnvironment with command-specific variables sets HasChanged to true
        /// Input: dictionary with keys prefixed with commandName, valid commandName
        /// Expected: HasChanged becomes true after command-specific variables are updated
        /// </summary>
        [Fact]
        public void UpdateEnvironment_CommandSpecificVariables_SetsHasChangedToTrue()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "mycommand_VAR1", "value1" },
                { "mycommand_VAR2", "value2" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "mycommand");

            // Assert
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with non-prefixed variables stores them in command environment and sets HasChanged
        /// Input: dictionary with keys not prefixed with commandName, valid commandName
        /// Expected: All entries stored in command environment, HasChanged is true
        /// </summary>
        [Fact]
        public void UpdateEnvironment_NonPrefixedVariables_StoredInCommandEnvironmentAndSetsHasChanged()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "VAR1", "value1" },
                { "VAR2", "value2" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "mycommand");

            // Assert
            var commandEnv = context.GetEnvironment("mycommand", false);
            Assert.Equal(2, commandEnv.Count);
            Assert.Equal("value1", commandEnv["VAR1"]);
            Assert.Equal("value2", commandEnv["VAR2"]);
            mockEnvironment.Verify(e => e.UpdateEnvironment(It.IsAny<Dictionary<string, string>>()), Times.Never);
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with mixed variables stores all entries in command-specific environment
        /// Input: dictionary with both prefixed and non-prefixed keys, valid commandName
        /// Expected: All entries stored in command environment, shared environment is not updated, HasChanged is true
        /// </summary>
        [Fact]
        public void UpdateEnvironment_MixedVariables_AllStoredInCommandEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "mycommand_VAR1", "cmdvalue1" },
                { "SHARED_VAR", "sharedvalue" },
                { "mycommand_VAR2", "cmdvalue2" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "mycommand");

            // Assert
            var commandEnv = context.GetEnvironment("mycommand");
            Assert.Equal(3, commandEnv.Count);
            Assert.Equal("cmdvalue1", commandEnv["mycommand_VAR1"]);
            Assert.Equal("cmdvalue2", commandEnv["mycommand_VAR2"]);
            Assert.Equal("sharedvalue", commandEnv["mycommand_SHARED_VAR"]);
            mockEnvironment.Verify(e => e.UpdateEnvironment(It.IsAny<Dictionary<string, string>>()), Times.Never);
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with case-insensitive prefix matching
        /// Input: dictionary with keys having different case variations of the command prefix
        /// Expected: All case variations match and are treated as command-specific
        /// </summary>
        [Theory]
        [InlineData("MyCommand", "mycommand_VAR")]
        [InlineData("MyCommand", "MYCOMMAND_VAR")]
        [InlineData("MyCommand", "MyCommand_VAR")]
        [InlineData("mycommand", "MYCOMMAND_VAR")]
        public void UpdateEnvironment_CaseInsensitivePrefixMatching_TreatsAsCommandSpecific(string commandName, string key)
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { key, "value" }
            };

            // Act
            context.UpdateEnvironment(dictionary, commandName);

            // Assert
            Assert.True(context.HasChanged);
            mockEnvironment.Verify(e => e.UpdateEnvironment(It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        /// <summary>
        /// Test: UpdateEnvironment creates new command-specific dictionary on first use
        /// Input: dictionary with command-specific variables, command not previously seen
        /// Expected: New ConcurrentDictionary is created and populated, removes prefix
        /// </summary>
        [Fact]
        public void UpdateEnvironment_FirstTimeCommandName_CreatesNewCommandDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "newcmd_VAR1", "value1" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "newcmd");

            // Assert
            var commandEnv = context.GetEnvironment("newcmd", false);
            Assert.Contains("VAR1", commandEnv.Keys);
            Assert.Equal("value1", commandEnv["VAR1"]);
        }

        /// <summary>
        /// Test: UpdateEnvironment updates existing command-specific variable
        /// Input: update same command-specific variable twice with different values
        /// Expected: Second update overwrites first value
        /// </summary>
        [Fact]
        public void UpdateEnvironment_UpdateExistingCommandVariable_OverwritesValue()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary1 = new Dictionary<string, string>
            {
                { "cmd_VAR", "oldvalue" }
            };
            var dictionary2 = new Dictionary<string, string>
            {
                { "cmd_VAR", "newvalue" }
            };

            // Act
            context.UpdateEnvironment(dictionary1, "cmd");
            context.UpdateEnvironment(dictionary2, "cmd");

            // Assert
            var commandEnv = context.GetEnvironment("cmd");
            Assert.Equal("newvalue", commandEnv["cmd_VAR"]);
        }

        /// <summary>
        /// Test: UpdateEnvironment with empty dictionary does not update any environment
        /// Input: empty dictionary, valid commandName
        /// Expected: No calls to _environment.UpdateEnvironment, HasChanged remains false
        /// </summary>
        [Fact]
        public void UpdateEnvironment_EmptyDictionary_DoesNotUpdateAnyEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>();

            // Act
            context.UpdateEnvironment(dictionary, "cmd");

            // Assert
            mockEnvironment.Verify(e => e.UpdateEnvironment(It.IsAny<Dictionary<string, string>>()), Times.Never);
            Assert.False(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment maintains isolation between different command names
        /// Input: variables for multiple different commands
        /// Expected: Each command has its own isolated environment dictionary
        /// </summary>
        [Fact]
        public void UpdateEnvironment_DifferentCommands_MaintainsIsolation()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dict1 = new Dictionary<string, string> { { "cmd1_VAR", "value1" } };
            var dict2 = new Dictionary<string, string> { { "cmd2_VAR", "value2" } };

            // Act
            context.UpdateEnvironment(dict1, "cmd1");
            context.UpdateEnvironment(dict2, "cmd2");

            // Assert
            var cmd1Env = context.GetEnvironment("cmd1");
            var cmd2Env = context.GetEnvironment("cmd2");

            Assert.Contains("cmd1_VAR", cmd1Env.Keys);
            Assert.DoesNotContain("cmd2_VAR", cmd1Env.Keys);

            Assert.Contains("cmd2_VAR", cmd2Env.Keys);
            Assert.DoesNotContain("cmd1_VAR", cmd2Env.Keys);
        }

        /// <summary>
        /// Test: UpdateEnvironment with whitespace commandName treats it as valid command
        /// Input: dictionary with variables, commandName with only whitespace
        /// Expected: Treated as valid command name (not caught by IsNullOrEmpty)
        /// </summary>
        [Fact]
        public void UpdateEnvironment_WhitespaceCommandName_TreatedAsValidCommand()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "  _VAR", "value" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "  ");

            // Assert
            // Should process as command-specific since IsNullOrEmpty returns false for whitespace
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with special characters in commandName
        /// Input: commandName with special characters, matching prefixed variables
        /// Expected: Variables with matching prefix are treated as command-specific
        /// </summary>
        [Fact]
        public void UpdateEnvironment_SpecialCharactersInCommandName_MatchesPrefixCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "cmd-test_VAR", "value" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "cmd-test");

            // Assert
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with very long commandName
        /// Input: commandName with 1000 characters, matching prefixed variable
        /// Expected: Handles long command name without error
        /// </summary>
        [Fact]
        public void UpdateEnvironment_VeryLongCommandName_HandlesCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var longCommandName = new string('a', 1000);
            var dictionary = new Dictionary<string, string>
            {
                { longCommandName + "_VAR", "value" }
            };

            // Act
            context.UpdateEnvironment(dictionary, longCommandName);

            // Assert
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with multiple command-specific variables
        /// Input: dictionary with multiple variables all prefixed with commandName
        /// Expected: All variables stored in command-specific environment, HasChanged is true
        /// </summary>
        [Fact]
        public void UpdateEnvironment_MultipleCommandSpecificVariables_AllStoredCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "cmd_VAR1", "value1" },
                { "cmd_VAR2", "value2" },
                { "cmd_VAR3", "value3" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "cmd");

            // Assert
            var commandEnv = context.GetEnvironment("cmd");
            Assert.Equal(3, commandEnv.Count);
            Assert.Equal("value1", commandEnv["cmd_VAR1"]);
            Assert.Equal("value2", commandEnv["cmd_VAR2"]);
            Assert.Equal("value3", commandEnv["cmd_VAR3"]);
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment does not modify HasChanged when already true
        /// Input: HasChanged already true, update with command-specific variables
        /// Expected: HasChanged remains true
        /// </summary>
        [Fact]
        public void UpdateEnvironment_HasChangedAlreadyTrue_RemainsTrue()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(true);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "cmd_VAR", "value" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "cmd");

            // Assert
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with key that partially matches prefix but not at start
        /// Input: key contains commandName but not as prefix
        /// Expected: Stored in command-specific environment along with all other entries
        /// </summary>
        [Fact]
        public void UpdateEnvironment_KeyContainsCommandNameButNotAsPrefix_StoredInCommandEnvironment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "PREFIX_cmd_VAR", "value" }
            };

            // Act
            context.UpdateEnvironment(dictionary, "cmd");

            // Assert
            var commandEnv = context.GetEnvironment("cmd", false);
            Assert.Equal("value", commandEnv["PREFIX_cmd_VAR"]);
            mockEnvironment.Verify(e => e.UpdateEnvironment(It.IsAny<Dictionary<string, string>>()), Times.Never);
            Assert.True(context.HasChanged);
        }

        /// <summary>
        /// Test: UpdateEnvironment with empty string values
        /// Input: dictionary with empty string values
        /// Expected: Empty values are stored correctly in the command-specific environment, prefix is not duplicated for command keys
        /// </summary>
        [Fact]
        public void UpdateEnvironment_EmptyStringValues_StoredCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.HasChanged).Returns(false);
            var context = new ControllerEnvironmentContext(mockEnvironment.Object);
            var dictionary = new Dictionary<string, string>
            {
                { "cmd_VAR1", string.Empty },
                { "SHARED_VAR", string.Empty }
            };

            // Act
            context.UpdateEnvironment(dictionary, "cmd");

            // Assert
            var commandEnv = context.GetEnvironment("cmd");
            Assert.Equal(string.Empty, commandEnv["cmd_VAR1"]);
            Assert.Equal(string.Empty, commandEnv["cmd_SHARED_VAR"]);
            mockEnvironment.Verify(e => e.UpdateEnvironment(It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        /// <summary>
        /// Test: Constructor with valid non-null parameters assigns environment and commandEnvironment correctly.
        /// Input: Valid IEnvironmentContext mock and populated ConcurrentDictionary.
        /// Expected: GetEnvironment returns data from the provided environment, GetEnvironment(commandName) returns data from the provided commandEnvironment.
        /// </summary>
        [Fact]
        public void Constructor_WithValidParameters_AssignsEnvironmentAndCommandEnvironment()
        {
            // Arrange
            var expectedEnvData = new Dictionary<string, string> { { "KEY1", "VALUE1" }, { "KEY2", "VALUE2" } };
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(expectedEnvData);

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var testCommandEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CMD_KEY1"] = "CMD_VALUE1",
                ["CMD_KEY2"] = "CMD_VALUE2"
            };
            commandEnvironment["testCommand"] = testCommandEnv;

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Assert
            var actualEnv = context.GetEnvironment();
            Assert.Equal(expectedEnvData, actualEnv);

            var actualCommandEnv = context.GetEnvironment("testCommand", false);
            Assert.Equal(2, actualCommandEnv.Count);
            Assert.Equal("CMD_VALUE1", actualCommandEnv["CMD_KEY1"]);
            Assert.Equal("CMD_VALUE2", actualCommandEnv["CMD_KEY2"]);
        }

        /// <summary>
        /// Test: Constructor with null environment parameter assigns null to _environment.
        /// Input: Null IEnvironmentContext and valid ConcurrentDictionary.
        /// Expected: GetEnvironment throws NullReferenceException when attempting to call methods on null _environment.
        /// </summary>
        [Fact]
        public void Constructor_WithNullEnvironment_AllowsNullAssignment()
        {
            // Arrange
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // Act
            var context = new ControllerEnvironmentContext(null!, commandEnvironment);

            // Assert
            Assert.Throws<NullReferenceException>(() => context.GetEnvironment());
        }

        /// <summary>
        /// Test: Constructor with null commandEnvironment parameter assigns null to _commandEnvironment.
        /// Input: Valid IEnvironmentContext and null ConcurrentDictionary.
        /// Expected: GetEnvironment(commandName) throws NullReferenceException when attempting to access null _commandEnvironment.
        /// </summary>
        [Fact]
        public void Constructor_WithNullCommandEnvironment_AllowsNullAssignment()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(new Dictionary<string, string>());

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, null!);

            // Assert
            Assert.Throws<NullReferenceException>(() => context.GetEnvironment("testCommand", false));
        }

        /// <summary>
        /// Test: Constructor with both parameters null assigns null to both properties.
        /// Input: Null IEnvironmentContext and null ConcurrentDictionary.
        /// Expected: GetEnvironment throws NullReferenceException.
        /// </summary>
        [Fact]
        public void Constructor_WithBothParametersNull_AllowsNullAssignment()
        {
            // Arrange & Act
            var context = new ControllerEnvironmentContext(null!, null!);

            // Assert
            Assert.Throws<NullReferenceException>(() => context.GetEnvironment());
        }

        /// <summary>
        /// Test: Constructor with empty commandEnvironment dictionary assigns empty dictionary correctly.
        /// Input: Valid IEnvironmentContext and empty ConcurrentDictionary.
        /// Expected: GetEnvironment(commandName) returns empty dictionary when commandName is not found.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyCommandEnvironment_AssignsEmptyDictionary()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(new Dictionary<string, string> { { "KEY", "VALUE" } });
            var emptyCommandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, emptyCommandEnvironment);

            // Assert
            var result = context.GetEnvironment("nonExistentCommand", false);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Test: Constructor with multiple commands in commandEnvironment assigns dictionary correctly.
        /// Input: Valid IEnvironmentContext and ConcurrentDictionary with multiple command-specific environments.
        /// Expected: GetEnvironment(commandName) returns correct environment data for each command.
        /// </summary>
        [Fact]
        public void Constructor_WithMultipleCommandsInCommandEnvironment_AssignsCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(new Dictionary<string, string>());

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            var cmd1Env = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CMD1_VAR"] = "VALUE1"
            };
            var cmd2Env = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CMD2_VAR"] = "VALUE2"
            };

            commandEnvironment["command1"] = cmd1Env;
            commandEnvironment["command2"] = cmd2Env;

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Assert
            var result1 = context.GetEnvironment("command1", false);
            Assert.Single(result1);
            Assert.Equal("VALUE1", result1["CMD1_VAR"]);

            var result2 = context.GetEnvironment("command2", false);
            Assert.Single(result2);
            Assert.Equal("VALUE2", result2["CMD2_VAR"]);
        }

        /// <summary>
        /// Test: Constructor assigns parameters correctly, preserving case-insensitive command lookup behavior.
        /// Input: Valid IEnvironmentContext and ConcurrentDictionary with command name in mixed case.
        /// Expected: GetEnvironment with different case variations returns the same command environment data.
        /// </summary>
        [Fact]
        public void Constructor_WithCommandEnvironment_PreservesCaseInsensitiveLookup()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(new Dictionary<string, string>());

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var cmdEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VAR"] = "VALUE"
            };
            commandEnvironment["TestCommand"] = cmdEnv;

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Assert
            var result1 = context.GetEnvironment("TestCommand", false);
            var result2 = context.GetEnvironment("testcommand", false);
            var result3 = context.GetEnvironment("TESTCOMMAND", false);

            Assert.Equal("VALUE", result1["VAR"]);
            Assert.Equal("VALUE", result2["VAR"]);
            Assert.Equal("VALUE", result3["VAR"]);
        }

        /// <summary>
        /// Test: Constructor with commandEnvironment containing empty nested dictionary assigns correctly.
        /// Input: Valid IEnvironmentContext and ConcurrentDictionary with a command that has an empty environment.
        /// Expected: GetEnvironment(commandName) returns empty dictionary for that command.
        /// </summary>
        [Fact]
        public void Constructor_WithCommandEnvironmentContainingEmptyNestedDictionary_AssignsCorrectly()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(new Dictionary<string, string>());

            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var emptyNestedEnv = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            commandEnvironment["emptyCommand"] = emptyNestedEnv;

            // Act
            var context = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Assert
            var result = context.GetEnvironment("emptyCommand", false);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Test: Constructor assigns unique instance properties correctly.
        /// Input: Valid IEnvironmentContext and ConcurrentDictionary.
        /// Expected: Each instance has a unique Id and default Name property.
        /// </summary>
        [Fact]
        public void Constructor_AssignsUniqueInstanceProperties()
        {
            // Arrange
            var mockEnvironment = new Mock<IEnvironmentContext>();
            mockEnvironment.Setup(e => e.GetEnvironment()).Returns(new Dictionary<string, string>());
            var commandEnvironment = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // Act
            var context1 = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);
            var context2 = new ControllerEnvironmentContext(mockEnvironment.Object, commandEnvironment);

            // Assert
            Assert.NotEqual(context1.Id, context2.Id);
            Assert.Equal("Controller Environment", context1.Name);
            Assert.Equal("Controller Environment", context2.Name);
        }
    }
}