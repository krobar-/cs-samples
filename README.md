# C# Code Samples

This repository contains a collection of C# code samples showcasing integration and functionality with external services and databases. Each sample demonstrates a different capability, from interacting with APIs to database management.

### Samples Included:
1. **Digital Measures Integration**
2. **Faculty and Staff Directory Management**
3. **Google Calendar API Integration**

Each code sample includes comments and explanations to aid understanding and demonstrate practical implementation techniques.

## Digital Measures Integration

This code demonstrates how to integrate and retrieve data from Digital Measures into a web application. It includes methods for:

- **Connecting to Digital Measures API:** Securely authenticates and fetches XML data.
- **Utility Functions:** Includes XML manipulation methods such as removing namespaces and sorting elements.
- **Encoding Management:** Methods for encoding conversion, ensuring compatibility.

## Faculty and Staff Directory 

This sample demonstrates a dynamic faculty and staff directory using a MySQL database backend. It includes features like:

- **Listing All Members:** Fetch and display a complete directory.
- **Filtering by Department or Category:** Retrieve directory listings filtered by specific departments or categories.

### Notable Components:
- **Group Management:** Handle groups of individuals effectively.
- **Dynamic Queries:** SQL queries are tailored to ensure modularity and flexibility.
- **Database Connection:** Securely manages MySQL connections using reusable components.

## Google Calendar API Integration

This code integrates with the Google Calendar API to fetch and process calendar events. It provides:

- **Authentication:** Uses a service account and certificate for secure connections.
- **Event Retrieval:** Fetches events based on filters like date range and category.
- **Dynamic Formatting:** Processes and displays event details in various formats.

### Features:
- Fetches and formats seminar schedules.
- Handles optional parameters like date ranges and order by fields.
- Provides tools for additional information extraction from event metadata.
