FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim
EXPOSE 5234
WORKDIR /app

# Copy the published output to the /app directory in the container.
# The pipeline publishes first: dotnet publish src/EmployeeData.TeamsBot.Presentation.Bot -c Release -o build/docker/publish
COPY publish /app

# Make sure the entry point executable is executable
RUN chmod +x ./EmployeeData.TeamsBot.Presentation.Bot

# Set the entrypoint to the executable for the app
ENTRYPOINT ["./EmployeeData.TeamsBot.Presentation.Bot"]
