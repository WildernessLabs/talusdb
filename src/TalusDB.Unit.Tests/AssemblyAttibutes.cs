
// dev note: Many of the unit tests use the same table and do add/remove count operations.
//           In order for these tests to remain vali, they must be run serially, not the default xUnit parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]