#!/bin/bash
# Test script to verify the fix for StatusCodeConventionRegistry

echo "Building AspNetCore project..."
dotnet build src/UnionGenerator.AspNetCore/UnionGenerator.AspNetCore.csproj -c Release --nologo

echo ""
echo "Building test project..."
dotnet build tests/UnionGenerator.AspNetCore.Tests/UnionGenerator.AspNetCore.Tests.csproj -c Release --nologo

echo ""
echo "Running specific test..."
dotnet test tests/UnionGenerator.AspNetCore.Tests/UnionGenerator.AspNetCore.Tests.csproj \
  --filter "FullyQualifiedName~StatusCodeConventionRegistryTests.Default_ContainsAllBuiltInConventions" \
  --no-build \
  -c Release \
  --verbosity normal

echo ""
echo "Done!"

