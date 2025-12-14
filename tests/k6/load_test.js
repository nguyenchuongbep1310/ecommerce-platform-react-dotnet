import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  stages: [
    { duration: "30s", target: 20 }, // Ramp up to 20 users
    { duration: "1m", target: 20 }, // Stay at 20 users
    { duration: "10s", target: 0 }, // Ramp down
  ],
  thresholds: {
    http_req_duration: ["p(95)<500"], // 95% of requests should be below 500ms
  },
};

export default function () {
  // Target the API Gateway exposed on host port 80
  const res = http.get("http://host.docker.internal:80/api/products");

  check(res, {
    "status is 200": (r) => r.status === 200,
  });

  sleep(1);
}
