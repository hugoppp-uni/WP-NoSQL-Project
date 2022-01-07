<template>
  <v-app>
    <v-app-bar
        app
        color="primary"
        dark
    >
      <div class="d-flex align-center">
        <v-img
            alt="Twitter Logo"
            class="shrink mr-2"
            contain
            src="./assets/twitter_logo.png"
            transition="scale-transition"
            width="40"
        />
        <h1>Twitter Hashtag Lookup</h1>
      </div>
    </v-app-bar>

    <v-main>
      <v-row>
<!--        Layout: Settings for User on Left Side, Query Results in Center and further information e.g. embedded Tweets on the right-->
      <v-col>
        <ApiSettings/>
      </v-col>
        <v-col>
          <QueryResult/>
        </v-col>
        <v-col>
          <OptionalResult/>
        </v-col>
      </v-row>
    </v-main>
  </v-app>
</template>

<script>
import ApiSettings from './components/ApiSettings';
import QueryResult from "./components/QueryResult";
import OptionalResult from "./components/OptionalResult";

export default {
  name: 'App',

  components: {
    ApiSettings: ApiSettings,
    QueryResult: QueryResult,
    OptionalResult: OptionalResult
  },

  data: () => ({
    //
  }),
};
</script>
