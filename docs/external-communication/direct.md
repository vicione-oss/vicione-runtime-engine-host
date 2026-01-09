# Direct

The data is distributed directly to engines running in the same process. When using the same `AssemblyLoadContext`, the data is passed directly. With different `AssemblyLoadContext`s, the data is passed via JSON.
