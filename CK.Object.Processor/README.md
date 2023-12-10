# CK.Object.Processor

A processor combines a condition on an object (a `Func<object,bool>` from
[CK.Object.Predicate](../CK.Object.Predicate/README.md)) and a transform function (a `Func<object,object>`
from [CK.Object.Transform](../CK.Object.Transform/README.md). The transform function is called
when the condition evaluates to true.

A processor is ultimately a `Func<object,object?>`: a `null` result captures the fact that the
condition failed.

## Configured Configuration and Transform and intrinsic ones.
Both the `Condition` and the `Transform` are optional: when they are not defined or results to
a (null) empty predicate and a (null) identity function, the processor is the (also null) void processor.

The [`ObjectProcessorConfiguration`](ObjectProcessorConfiguration.cs) is a concrete class:
by configuring its `Condition` with a predicate and its `Transform` with a transform function, it
is operational.

However, most often, we specialize this base class and set the `IntrinsicCondition`
and `IntrinsicTransform`. These methods can implement any
condition or transformation in addition to the potentially configured ones. Below is a
(rather stupid) processor that negates a double:
```csharp
public sealed class NegateDoubleProcessorConfiguration : ObjectProcessorConfiguration
{
    public NegateDoubleProcessorConfiguration( IActivityMonitor monitor,
                                                TypedConfigurationBuilder builder,
                                                ImmutableConfigurationSection configuration,
                                                IReadOnlyList<ObjectProcessorConfiguration> processors )
        : base( monitor, builder, configuration, processors )
    {
        SetIntrinsicCondition( Condition );
        SetIntrinsicTransform( Transform );
    }

    Func<object, bool>? Condition( IActivityMonitor monitor, IServiceProvider services )
    {
        return static o => o is double;
    }

    Func<object, object>? Transform( IActivityMonitor monitor, IServiceProvider services )
    {
        return static o => -((double)o);
    }
}
```
The execution order is as follow:
- First, the intrinsic condition is tested. If it fails, the processor returns null.
- Then the configured `Condition` is tested. If it fails, the processor returns null.
- Once accepted, the object is transformed:
  - First, the intrinsic transform is applied.
  - Then the configured `Transform` is applied to the intrinsic result.

## `ObjectProcessorConfiguration` is its own composite.
The `ObjectProcessorConfiguration` is the base type AND the default composite
for this family type. Any processor can have subordinated processors.

It is defined by the "Processors" configuration key:
```jsonc
{
    "Assemblies": { "CK.Object.Processor.Tests": "Test"},
    "Processors": [/*...*/]
}
```
The inner "Processors" is a "first-wins" list of processors (a kind of switch-case).
When there are "Processors", the execution order is as follow:

- First, the intrinsic condition is tested. If it fails, the sequence processor returns null.
- Then the configured `Condition` is tested. If it fails, the sequence processor returns null.
- Once this first check done:
   - The accepted object is submitted to the subordinated processors.
   - If none of them processed it, the sequence processor returns null.
   - If the object has been processed, then we apply the final transformation to the processed
     result:
     - First, the intrinsic transform is applied.
     - Then the configured `Transform` is applied to the intrinsic result.

## `ObjectProcessorConfiguration` handle synchronous and asynchronous dynamically.
[CK.Object.Predicate](../CK.Object.Predicate/README.md)) and
[CK.Object.Transform](../CK.Object.Transform/README.md) are structurally purely asynchronous
or asynchronous and synchronous. Processors are different, they adapt their behavior depending
on their components.

If a processor can be synchronous because all its components are synchronous
it will allow to create a synchronous processor: its `bool IsSynchronous { get; }` property exposes
this and `Func<object,object?> CreateProcessor( monitor, services )` can be called to obtain
an efficient function.  

When `IsSynchronous` is false, `CreateProcessor` throws an `InvalidOperationException`. It is
`Func<object,ValueTask<object?>> CreateAsyncProcessor( monitor, services )` (this one can be always
be called) that must be called to obtain the asynchronous processor function.

When mutation occur (by replacing placeholders), a processor with a true `IsSynchronous` can be
mutated in a processor that has no more the capability of generating purely synchronous functions.
This can happen because an async predicate or async transform appeared somewhere in the new
configuration.




