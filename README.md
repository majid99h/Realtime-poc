# TrafficRealtime.RealtimeArchitecture.Poc

Proof of Concept for a scalable RabbitMQ → .NET → SignalR realtime architecture.

The purpose of this POC is to validate a new realtime communication and event-routing model before integrating it into the existing production Traffic Realtime project.

---

## 1. Problem

The existing realtime system supports multiple channels:

- Transaction
- Alarm
- Watchlist
- Violation
- Future realtime channels

Users can select different cameras for each channel.

For example:

User A:

    Transaction → Camera 1, 2, 3

User B:

    Transaction → Camera 3, 4, 5

User C:

    Watchlist → Camera 12, 13

The system needs to support:

- Many users
- Many cameras
- User-specific camera selection
- Multiple realtime channels
- Multiple API servers
- High RabbitMQ event rates

The main concerns are:

1. Too many SignalR connections.
2. Repeatedly checking every connected user for every event.
3. Incorrect filtering when the same camera is used by different channels.
4. High processing cost when the number of users and cameras increases.
5. Supporting multiple API servers while RabbitMQ remains the main realtime source.

---

# 2. Proposed Architecture

The POC introduces a direct subscription index:

    Channel + Camera → Connections

The overall architecture is:

    RabbitMQ
        ↓
    .NET RabbitMQ Consumer
        ↓
    Realtime Dispatcher
        ↓
    Channel + Camera Index
        ↓
    Matching SignalR Connections
        ↓
    Angular Realtime Service
        ↓
    UI Components

---

# 3. Core Concept

Instead of checking every connected user when an event arrives:

    Event Camera 3
        ↓
    Check User 1
    Check User 2
    Check User 3
    ...
    Check User N

The dispatcher maintains an index.

Example:

    Transaction + Camera 3
        ↓
    Connection A
    Connection B

    Transaction + Camera 7
        ↓
    Connection C

    Watchlist + Camera 12
        ↓
    Connection A
    Connection B

When:

    Transaction + Camera 3

arrives, the dispatcher directly finds:

    Connection A
    Connection B

and sends the event only to those connections.

---

# 4. Why Channel + Camera?

Camera ID alone is not enough.

The same camera can produce different realtime events:

    Transaction + Camera 3
    Alarm + Camera 3
    Watchlist + Camera 3
    Violation + Camera 3

Therefore the subscription key is:

    Channel + Camera

This prevents events from one channel being delivered to users subscribed to another channel.

---

# 5. SignalR Connection Strategy

The client uses one SignalR connection per dashboard/client.

For example, a dashboard may contain:

- Live Transaction
- Alarm
- Watchlist
- Violation
- Map
- Heatmap
- Congestion

The target architecture is:

    Angular Dashboard
            ↓
    RealtimeSignalrService
            ↓
    ONE SignalR Connection
            ↓
    Multiple Channel Subscriptions

The application does NOT create one SignalR connection for each camera.

---

# 6. 1,000 Camera Test

The POC was tested with 1,000 cameras.

The client sends:

    UpdateChannel(
        "Transaction",
        [1 ... 1000]
    )

The result is:

    1 SignalR WebSocket
    +
    1 subscription containing 1,000 camera IDs

Therefore:

    1,000 cameras ≠ 1,000 SignalR connections

This is one of the main objectives of the POC.

---

# 7. User-Specific Camera Selection

Each connection can have its own camera selection.

Example:

    User A
    Transaction → 1, 2, 3

    User B
    Transaction → 3, 4, 5

    User C
    Transaction → 7, 8

The index becomes:

    Transaction + Camera 1 → A
    Transaction + Camera 2 → A
    Transaction + Camera 3 → A, B
    Transaction + Camera 4 → B
    Transaction + Camera 5 → B
    Transaction + Camera 7 → C
    Transaction + Camera 8 → C

---

# 8. RabbitMQ Responsibility

RabbitMQ remains the primary realtime event source.

RabbitMQ is responsible for:

    Receiving/publishing realtime events
    ↓
    Delivering events to API consumers

The API is responsible for deciding which connected users should receive each event.

The responsibilities are therefore separated:

    RabbitMQ
        ↓
    Event distribution to API instances

    .NET
        ↓
    User/camera/channel filtering

    SignalR
        ↓
    Browser delivery

---

# 9. Multiple API Servers

The architecture supports multiple API instances.

Example:

    RabbitMQ
        │
        ├───────────────┐
        ↓               ↓
    API Server 1     API Server 2
       :5168            :5169
        │               │
     SignalR          SignalR
        │               │
      Users           Users

Each API server maintains the subscriptions for the SignalR connections connected to that server.

Each server can consume the same realtime source through its own RabbitMQ queue.

Example:

    Server 1
    qu.monitor.live.poc.server1

    Server 2
    qu.monitor.live.poc.server2

This allows the API layer to scale horizontally.

---

# 10. Angular Client Architecture

The Angular client uses a shared realtime service.

Target architecture:

    RealtimeSignalrService
            │
            ├── Transaction
            ├── Alarm
            ├── Watchlist
            ├── Violation
            ├── Map
            ├── Heatmap
            └── Congestion

Components consume realtime events from the shared service instead of creating their own SignalR connections.

---

# 11. POC Tests

The following scenarios have been validated:

| Test | Result |
|---|---|
| Multiple realtime channels | Passed |
| User-specific camera subscriptions | Passed |
| Same camera subscribed by multiple users | Passed |
| Different camera selections per user | Passed |
| Channel-specific filtering | Passed |
| 1,000 camera subscription | Passed |
| One SignalR connection for 1,000 cameras | Passed |
| Channel + Camera → Connection index | Passed |
| RabbitMQ → .NET → SignalR routing | Passed |
| Multiple API servers | Final validation |

---

# 12. Example Routing Test

The POC produced the following routing results:

    Transaction + Camera 3 → Subscribers = 2

    Transaction + Camera 4 → Subscribers = 1

    Transaction + Camera 7 → Subscribers = 1

    Watchlist + Camera 12 → Subscribers = 2

    Watchlist + Camera 13 → Subscribers = 1

    Watchlist + Camera 20 → Subscribers = 1

This confirms that the dispatcher sends events only to matching connections.

---

# 13. Benefits

## Fewer SignalR Connections

One dashboard can use one SignalR connection for multiple channels and camera subscriptions.

## Efficient Event Routing

The backend can directly find matching connections instead of scanning all connections.

## Correct Filtering

Using:

    Channel + Camera

prevents cross-channel event delivery.

## User-Specific Subscriptions

Every user can select a different set of cameras.

## Better Scalability

The architecture is designed to support increasing numbers of:

- Users
- Cameras
- Channels
- API servers

## Easier Future Development

New realtime channels can use the same subscription and dispatching model.

---

# 14. What This POC Solves

The POC solves the architectural problem of:

    "How do we efficiently deliver a realtime camera event
     only to the users who are interested in that camera
     and channel?"

The answer is:

    Channel + Camera
          ↓
    Subscription Index
          ↓
    Matching Connections
          ↓
    SignalR

---

# 15. What This POC Does Not Yet Prove

This POC validates the architecture and routing model.

It is not a complete production load test.

The following still need to be tested in a controlled environment:

- Maximum events per second
- Large number of concurrent users
- Large number of concurrent cameras
- Browser rendering performance
- Angular change detection under high event rates
- Memory usage
- CPU usage
- Long-running SignalR connections
- Reconnect scenarios
- Redis/shared-state behavior where required
- Full two-server production-style load testing

---

# 16. Future Performance Optimization

The architecture separates event routing from UI rendering.

If event volume becomes very high, additional optimization can be added without changing the core routing architecture.

Possible optimizations include:

- Event batching
- Throttling
- Buffering
- RxJS operators
- Angular change detection optimization
- Map update batching
- Heatmap update intervals
- Congestion aggregation
- Client-side event queues

The important point is that these optimizations can be added later without creating additional SignalR connections.

---

# 17. Production Integration Plan

After final POC validation, integrate the architecture into the existing production project.

The existing RabbitMQ infrastructure should be reused where practical.

The primary architectural change will be the realtime subscription and dispatching layer.

Target production flow:

    RabbitMQ
        ↓
    Existing .NET RabbitMQ Consumer
        ↓
    Realtime Dispatcher
        ↓
    Channel + Camera Index
        ↓
    SignalR
        ↓
    Angular Realtime Service
        ↓
    UI Components

---

# 18. Simple Explanation for the Team

Instead of opening many SignalR connections and checking every user for every camera event, we keep one SignalR connection per dashboard and maintain a direct:

    Channel + Camera → Connection

index.

When an event arrives, the system directly finds the users who need that event and sends it only to them.

---

# 19. Key Takeaway

The POC demonstrates that the realtime system can move from:

    RabbitMQ Event
        ↓
    Check Every Connection
        ↓
    Find Matching Cameras
        ↓
    Send Event

to:

    RabbitMQ Event
        ↓
    Channel + Camera
        ↓
    Direct Connection Lookup
        ↓
    Send Only to Matching Users

This reduces unnecessary processing, avoids unnecessary SignalR connections, provides correct user-specific filtering, and gives us a scalable foundation for future realtime channels.

---

## Repository

Recommended repository name:

    TrafficRealtime.RealtimeArchitecture.Poc

Recommended short description:

    POC for scalable RabbitMQ + SignalR realtime architecture using Channel + Camera connection indexing and single SignalR connections per client.
