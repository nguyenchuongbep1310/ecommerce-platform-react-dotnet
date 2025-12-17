import { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';

const SignalRContext = createContext();

export const useSignalR = () => useContext(SignalRContext);

export const SignalRProvider = ({ children }) => {
    const { user } = useAuth();
    const [connection, setConnection] = useState(null);
    const [notifications, setNotifications] = useState([]);

    useEffect(() => {
        if (!user) {
            setConnection(null);
            return;
        }

        const token = localStorage.getItem('token');
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub", {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, [user]);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('SignalR Connected!');
                    // Join the user's group
                    if (user && user.id) {
                        connection.invoke("JoinGroup", user.id.toString())
                            .catch(err => console.error("JoinGroup failed", err));
                    }
                    
                    connection.on("ReceiveOrderNotification", (message) => {
                        console.log("Notification received:", message);
                        setNotifications(prev => [...prev, message]);
                        // You could trigger a toast here
                        alert(`Order Processed! ID: ${message.orderId}, Total: $${message.totalAmount}`);
                    });
                })
                .catch(e => console.log('Connection failed: ', e));
                
            return () => {
                connection.stop();
            };
        }
    }, [connection, user]);

    return (
        <SignalRContext.Provider value={{ connection, notifications }}>
            {children}
        </SignalRContext.Provider>
    );
};
