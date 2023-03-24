import {useEffect, useState} from "react";
import {AppProps, ChangeReason, ContainerProps, Post, PostProps, SocketState, SocketStateEnum} from "./types";

export default function App({ port }: AppProps) {
  
  const [socket, setSocket] = useState<WebSocket>();

  useEffect(() => {
    setSocket(new WebSocket("ws://localhost:" + port + "/"));
    return () => {
      socket?.close();
    }
  }, [setSocket]);

  return socket == undefined ? <span>Loading</span> :  <MainContainer socket={socket!} />;
}

function MainContainer({ socket }: ContainerProps) {
  
  const [socketState, setSocketState] = useState<SocketState>({state: SocketStateEnum.PENDING, message: "Idle."});
  const [socketIndex, setSocketIndex] = useState<Map<number, Date>>(new Map<number, Date>([]));
  const [socketPosts, setSocketPosts] = useState<Map<number, Post>>(new Map<number, Post>([]));
  
  useEffect(() => {
    socket.onopen = () => {
      console.log("Established connection to websocket server.");
      setSocketState( {state: SocketStateEnum.PENDING, message: "Connecting..."} );
    }

    socket.close = () => {
      console.warn("Websocket connection has been closed.");
      setSocketState( {state: SocketStateEnum.CLOSED, message: "Connection closed."} );
    }

    socket.onerror = e => {
      console.warn("Websocket connection has encountered an error!");
      setSocketState( {state: SocketStateEnum.ERROR, message: "An Error has occured. Check the console for errors."} );
    }

    socket.onmessage = evt => {
      let data: any = JSON.parse(evt.data);
      console.info("Message received: " + data.kind);
      if(data.kind == "index") {
        const index = new Map(Object.entries(data.data).map(([key, value]) => [Number(key), new Date(value as number)]))
        setSocketIndex(index);
        console.log("Index data received, updating posts...");
        updatePosts(Array.from(index.keys()));
        console.log("Posts fetched.");
        setSocketState( {state: SocketStateEnum.OPEN, message: "Connected."} );
      } else if(data.kind == "update") {
        if(socketState.state == SocketStateEnum.PENDING) {
          setSocketState( {state: SocketStateEnum.ERROR, message: "Received update without having received a initial index."} );
        } else {
          console.log("Updating index...");
          let ids = updateIndex(data.data);
          console.log("Updating posts...");
          updatePosts(ids);
          console.log("Done.");
        }
      } else if(data.kind == "post") {
        console.log("Received post for " + data.data.id);
        let posts = new Map(socketPosts);
        posts.set(data.data.id, data.data.post);
        setSocketPosts(posts);
      } else if(data.kind == "message") {
        console.log(data.data);
      } else {
        console.log("Unknown data received: " + JSON.stringify(data))
      }
      
      function updatePosts(ids: number[]) {
        ids.forEach(i => {
          console.log("Fetching " + i);
          socket.send(String(i));
        });
      }
      
      function updateIndex(entries: {reason: ChangeReason, data: any}[]): number[] {
        let ids: number[] = [];
        entries.forEach((d) => {
          switch(d.reason) {
            case ChangeReason.MODIFIED:
            case ChangeReason.ADDED:
              socketIndex.set(d.data.id, d.data.date);
              ids.push(d.data.id);
              break;
            case ChangeReason.REMOVED:
              socketIndex.delete(d.data.id);
              break;
          }
        });
        return ids;
      }
    }
  }, [socketState, socketIndex, socketPosts, setSocketIndex, setSocketState, setSocketPosts]);

  console.log(socketPosts)
  let posts: JSX.Element[] = [];
 [...socketPosts].forEach(([key, value], index) => {
    console.log("Ye")
    posts.push(<DataPostContainer id={+key} date={socketIndex.get(+key)!} post={value} key={index}/>);
  });
  console.log(posts)
  
  return <div className="mainContent">
    <div className="socketStatus">
      Status: <span>{socketState?.state}</span> | <span>{socketState?.message}</span> <button onClick={() => console.log("click")}>Refresh Now</button>
    </div>
    <div className="posts">
      {posts.length == 0 ? "No data found." : posts}
    </div>
  </div>;
}

function DataPostContainer({ id, date, post }: PostProps) {
  let table: JSX.Element[] = [...post.count].map(([key, value], index) => {
    return <tr key={index}>
      <td>{key}</td>
      <td>{value}</td>
    </tr>;
  });
  
  return <div className="post">
    <h3>{post.title}</h3> | <span>{date.toTimeString()}</span><br/>
    <div className="contentCount">
      <table>
        <tr>
          <th>Wort</th>
          <th>Anzahl</th>
        </tr>
        {table}
      </table>
    </div>
  </div>
}