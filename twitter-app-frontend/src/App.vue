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
        <v-col>
          <ApiSettings/>
        </v-col>
        <v-col>
          <TrendingHashtags/>
        </v-col>
        <v-col>
          <SimilarHashtags/>
        </v-col>
      </v-row>
    </v-main>
  </v-app>
</template>

<script>
import ApiSettings from './components/ApiSettings';
import TrendingHashtags from "./components/TrendingHashtags";
import SimilarHashtags from "./components/SimilarHashtags";

export default {
  name: 'App',

  components: {
    ApiSettings: ApiSettings,
    TrendingHashtags: TrendingHashtags,
    SimilarHashtags: SimilarHashtags
  },

  data: () => ({
    //
  }),
};
</script>
