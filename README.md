# Mealie_To_Todoist

**Mealie_To_Todoist** is a .NET 9 application that synchronizes your [Mealie](https://mealie.io/) shopping list with [Todoist](https://todoist.com/). It ensures your shopping items are always up-to-date in Todoist, reflecting changes made in Mealie and vice versa.

## How It Works

The core logic is handled by the `SyncService` class, which:

- Fetches your shopping list from Mealie.
- Fetches your tasks from Todoist.
- Adds new Mealie items to Todoist if they don't exist.
- Updates Todoist tasks if the corresponding Mealie item changes.
- Marks items as complete in Todoist and/or Mealie as appropriate.
- Optionally deletes completed items from Mealie, based on your settings.

This keeps your shopping list in sync across both platforms.

## Running with Docker

You can run the app using Docker Compose. Below is a sample configuration:

```

services:
  mealietotodoist:
    image: ghcr.io/rossdargan/mealie_to_todoist:0.3.2-alpha
    restart: unless-stopped
    ports:
      - 8888:8080
    environment:
      - Settings__MealieBaseUrl=https://mealie.example.com
      - Settings__MealieApiKey={mealiekey}
      - Settings__TodoistApiKey={todoistkey}
      - Settings__TodoistShoppingListName=Shopping
networks: {}
```

### Required Environment Variables

- `Settings__MealieBaseUrl`: The base URL of your Mealie instance (e.g., `https://mealie.example.com`).
- `Settings__MealieApiKey`: Your Mealie API key.
- `Settings__TodoistApiKey`: Your Todoist API key.
- `Settings__TodoistShoppingListName`: The name of the Todoist project to sync with (default: `Shopping`).

#### How to Get Your Secrets

- **Mealie API Key:**  
  Log in to your Mealie instance, go to your user profile, and generate an API key.

- **Todoist API Key:**  
  Log in to Todoist, go to [Integrations](https://todoist.com/prefs/integrations), and copy your API token.

## Triggering Syncs from Mealie

To keep Todoist updated on demand, you can add a [Notifier](https://hay-kot.github.io/mealie/features/notifications/) to Mealie:

1. Go to your Mealie admin panel.
2. Navigate to **Settings > Notifiers**.
3. Add a new notifier of type **apprise**.
4. Set the URL to your Mealie_To_Todoist instance (e.g., `json://<your-server>:<port>/notification`).
5. Configure the notifier to trigger on shopping list changes.

This will call the sync endpoint whenever your shopping list is updated, ensuring Todoist stays in sync.

## License

MIT

## Contributing

Contributions are welcome! Please open issues or pull requests as needed.
