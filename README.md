# <img src="https://i.imgur.com/BOExs52.png" width="25" style="margin: 5px 0px 0px 10px"/> Nucs.Essentials
[![Nuget downloads](https://img.shields.io/nuget/vpre/Nucs.Essentials.svg)](https://www.nuget.org/packages/Nucs.Essentials/)
[![NuGet](https://img.shields.io/nuget/dt/Nucs.Essentials.svg)](https://github.com/Nucs/Nucs.Essentials)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/Essentials/blob/master/LICENSE)

If you had a bunch of high performance classes, would you not place them in a nuget package?<br/>
This library contains essential classes I use in production.<br/>
Cloning and exploring this repository is the recommended way of learning how to use it.

### Installation
Supports `netcoreapp3.1` `net6.0` `net7.0`
```sh
PM> Install-Package Nucs.Essentials
```
Overview
---
All performance-oriented classes have a benchmark at [Nucs.Essentials.Benchmark](https://github.com/Nucs/Nucs.Essentials/tree/main/benchmark/Nucs.Essentials.Benchmarks) project and usually unit-tested.<br/>

### Text
- [LineReader](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Streams/LineReader.cs) ([faster by 185% than string.Split](https://github.com/Nucs/Nucs.Essentials/blob/main/benchmark/Nucs.Essentials.Benchmarks/LineReaderBenchmark.cs) and [parses 25% faster than with string.Split](https://github.com/Nucs/Nucs.Essentials/blob/main/benchmark/Nucs.Essentials.Benchmarks/LineReaderParseBenchmark.cs))
  allows splitting a string without creating a copy (using `Span<char>`) with any kind of separator. Useful for csv parsing.
- [RowReader](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Streams/RowReader.cs) ([faster by 215% than string.Split](https://github.com/Nucs/Nucs.Essentials/blob/main/benchmark/Nucs.Essentials.Benchmarks/RowReaderBenchmark.cs)) 
  similar to LineReader but specializes in row splitting without copy (using `Span<char>`).
- [StreamRowReader](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Streams/StreamRowReader.cs)
    similar to RowReader but for streams with an automated buffer algorithm.
- [ValueStringBuilder](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Text/ValueStringBuilder.cs) 
  allows building strings using pooled buffers, useful for high performance string building.
- [ReverseLineReader](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Streams/StreamRowReader.cs)
  for reading lines from the end of a file.

### Collections
- [RollingWindow\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/RollingWindow.cs)
  a rolling window (list) of fixed size. When full, last one pops and new item is pushed to front, useful for statistics.
- [StructList\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/Structs/StructList.cs) and [StructQueue\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/Structs/StructQueue.cs) 
  are struct port of `List<T>`/`Queue<T>` with additional functionalities such as exposing internal fields and deconstructors, essentially allowing a very versatile use of them.
  Versioning to protect against multithreaded access has been removed.
- Reusable queues for wrapping [List\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/Structs/ReusableListQueue.cs) / [Array](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/Structs/ReusableArrayQueue.cs) / [ReadOnlySpan\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/Structs/ReusableSpanQueue.cs) 
  allowing reuse/resetting the queue without needing to create a new instance. Also exposes functionalities such as Peak and iteration.

### Multithreading / Collections
- [Async](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/AsyncSingleProducerSingleConsumerQueue.cs) / [SingleProducerSingleConsumerQueue\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/SingleProducerSingleConsumerQueue.cs) 
  a high performance lockless queue for single producer and single consumer with awaitable signal for available read [faster than System.Threading.Channels by 81%](https://github.com/Nucs/Nucs.Essentials/blob/main/benchmark/Nucs.Essentials.Benchmarks/AsyncSingleProducerSingleConsumerQueue_EnqueueDequeue_Benchmark.cs).
- [Async](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/AsyncManyProducerManyConsumerStack.cs) / [ManyProducerManyConsumerStack\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/ManyProducerManyConsumerStack.cs)
  a high performance lockless stack using linked-list for many producers and many consumers with awaitable signal for available read.
- [AsyncRoundRobinProducerConsumer\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/AsyncRoundRobinProducerConsumer.cs)
  a lockless round-robin channel that accepts data from multiple producers and distributes it to multiple `AsyncSingleProducerSingleConsumerQueue` consumers. This pattern allows feeding `<T>` to multiple consumers without locking.
- [AsyncCountdownEvent](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Threading/AsyncCountdownEvent.cs) 
  a lockless countdown event that allows awaiting for a specific number of signals. awaiting completes once 0 is reached. Counter can be incremented and decremented. Serves like a `SemaphoreSlim` that awaits for reaching 0 signals remaining.
- [ConcurrentPriorityQueue\<TKey, TValue\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/ConcurrentPriorityQueue.cs)
  lock based priority queue based on generic key ordering priority by a `IComparable\<TKey\>`.
- [ConcurrentHashSet\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/ConcurrentHashSet.cs)
    bucket-based locking (multiple locks, depending on hash of the item, better than single-lock) with Dictionary-like buckets hashset for concurrent access.
- [ObservableConcurrentList](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Collections/ObservableConcurrentList.cs)
    a thread-safe observable list that notifies changes using `INotifyCollectionChanged`. Useful for concurrent WPF binding. Allows transactions using `IDisposable BlockReentrancy()` that on dispose will notify changes.

### Reflection / Generators / Expressions
All expression related classes have an overload for `Expression` and a `Delegate`.
- [DictionaryToSwitchCaseGenerator](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Reflection/DictionaryToSwitchCaseGenerator.cs)
  creates a switch-case expression from a dictionary of `TKey` to `TValue` and a default value case. Essentially inlines a dictionary into a switch-case as a `Func<TKey, TValue>`.
- [PreloadedPropertyGetter](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Reflection/PreloadedPropertyGetter.cs)
  generates a getter for all properties of a type and caches it for future use. Useful for reflection-heavy code.
- [StructToString](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Reflection/StructToString.cs)
  generates a `ToString` method for a struct that returns a string. Used to avoid a mistake of using `object.ToString` that forces a cast from struct to object.
- [ToDictionaryGenerator](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Reflection/ToDictionaryGenerator.cs)
  generates a `ToDictionary` method for a target type `<T>` that returns a `Dictionary<string, object>` of all properties. Supports boxing of struct/primitive values via `PooledStrongBox<T>`. Useful for destructing an object into a dictionary.
- [DefaultValue\<T\>](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Essentials/Reflection/DefaultValue.cs)
  provides a method to create a default value of a `<T>`. As-well as a cached boxed value and `T` value.


# <img src="https://i.imgur.com/BOExs52.png" width="25" style="margin: 5px 0px 0px 10px"/> Nucs.Optimization
[![Nuget downloads](https://img.shields.io/nuget/vpre/Nucs.Optimization.svg)](https://www.nuget.org/packages/Nucs.Essentials/)
[![NuGet](https://img.shields.io/nuget/dt/Nucs.Optimization.svg)](https://github.com/Nucs/Nucs.Essentials)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/Essentials/blob/master/LICENSE)


A .NET binding using [pythonnet](https://github.com/pythonnet/pythonnet) for [skopt (scikit-optimize)](https://scikit-optimize.github.io/) - an optimization library with support to dynamic search spaces through generic binding.<br/>

Available Algorithms:
- [x] [Random Search](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyRandomOptimization.cs)
- [x] [Bayesian Optimization](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyBayesianOptimization.cs)
- [x] [Random Forest Optimization](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyForestOptimization.cs)
- [x] [Gradient Boosting Regression Trees (Gbrt)](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyGbrtOptimization.cs)

*Source code can be, [found here](https://github.com/Nucs/Nucs.Essentials/tree/main/src/Nucs.Optimization)*


### Installation

* Python 3.8+
    ```sh
    numpy>=1.23.5
    pythonnet>=3.0.1
    scikit-learn>=1.2.0
    scikit-optimize>=0.9.0
    scipy>=1.9.3
  
    > pip install numpy pythonnet scikit-learn scikit-optimize scipy
    ```
* .NET 7.0
    ```sh
    PM> Install-Package Nucs.Optimization
    ```

### Getting Started


Declare a parameters class/record for the optimization search space.</br>
Annotate it with IntegerSpace / RealSpace / CategoricalSpace attributes. </br>
Non-annotated parameters will be implicitly included by default.

```C#

[Parameters(Inclusion = ParametersInclusion.ImplicitAndExplicit)] //include all annotated and non-annotated
public record Parameters {
    [IntegerSpace<int>(1, int.MaxValue, Prior = Prior.LogUniform, Base = 2, Transform = NumericalTransform.Normalize)]
    public int Seed; //range of 0 to int.MaxValue (including)

    [RealSpace<double>(0, Math.PI)]
    public double FloatSeed; //range of 0 to int.MaxValue (including)

    [CategoricalSpace<float>(1f, 2f, 3f)]
    public float NumericalCategories { get; set; } //one of 1f, 2f, 3f

    [CategoricalSpace<double>(1d, 10d, 100d, 1000d)]
    public double LogNumericalCategories { get; set; } //one of 1d, 10d, 100d, 1000d

    [CategoricalSpace<string>("A", "B", "C", Transform = CategoricalTransform.Identity)]
    public string Categories; //one of "A", "B", "C"

    [CategoricalSpace<bool>] //optional, will be included implicitly
    public bool UseMethod; //true or false

    [CategoricalSpace<SomeEnum>(SomeEnum.A, SomeEnum.B, SomeEnum.C)]
    public SomeEnum AnEnum; //one of the enum values ("A", "B", "C")

    /// string will be parsed to SomeEnum. Prior provides the priority of each possible value. 'B' will have 80% priority of being selected.
    [CategoricalSpace<SomeEnum>("A", "B", Prior = new double[] {0.2, 0.8})] 
    public SomeEnum AnEnumWithValues; //one of the enum values ("A", "B")

    public SomeEnum AllValuesOfEnum; //one of any of the values of the enum
        
    [IgnoreDataMember]
    public bool Ignored; //will be ignored entirely
}

public enum SomeEnum { A, B, C }

```

```C#
//setup python runtime
Runtime.PythonDLL = Environment.ExpandEnvironmentVariables("%APPDATA%\\..\\Local\\Programs\\Python\\Python38\\python38.dll");
PythonEngine.Initialize();
PythonEngine.BeginAllowThreads();
using var py = Py.GIL(); //no GIL is being taken inside. has to be taken outside.

//declare a function to optimize
[Maximize] //or [Minimize]
double ScoreFunction(Parameters parameters) {
    return (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1) * Math.Sin(0.05+parameters.FloatSeed)) / 1000000;
}

//construct an optimizer
var opt = new PyBayesianOptimization<Parameters>(ScoreFunction);
var opt2 = new PyForestOptimization<Parameters>(ScoreFunction);
var opt3 = new PyRandomOptimization<Parameters>(ScoreFunction);
var opt4 = new PyGbrtOptimization<Parameters>(ScoreFunction);

//(optional) prepare callbacks
var callbacks = new PyOptCallback[] { new IterationCallback<Parameters>(maximize: true, (iteration, parameters, score) => {
    Console.WriteLine($"[{iteration}] Score: {score}, Parameters: {parameters}");
})};

//run optimizer of choice (Search, SearchTop, SearchAll)
double Score;
Parameters Parameters;
(Score, Parameters) = opt.Search(n_calls: 100, n_random_starts: 10, verbose: false, callbacks: callbacks);
(Score, Parameters) = opt2.Search(n_calls: 100, n_random_starts: 10, verbose: false, callbacks: callbacks);
(Score, Parameters) = opt3.Search(n_calls: 100, verbose: false, callbacks: callbacks);
(Score, Parameters) = opt4.Search(n_calls: 100, n_random_starts: 10, verbose: false, callbacks: callbacks);
```
